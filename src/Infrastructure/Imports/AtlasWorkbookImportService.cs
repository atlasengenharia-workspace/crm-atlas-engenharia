using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Operacao;
using CrmAtlas.ApplicationCore.Servicos;
using CrmAtlas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CrmAtlas.Infrastructure.Imports;

public sealed partial class AtlasWorkbookImportService(
    AtlasDbContext db,
    IAcompanhamentoSpreadsheetReader operationalReader) : IAtlasWorkbookImportService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<AtlasWorkbookImportResult> ImportAsync(
        Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (!Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A importação integrada requer um arquivo .xlsx.");

        await using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes, cancellationToken);
        var content = bytes.ToArray();
        var operational = await operationalReader.ReadAsync(
            new MemoryStream(content, writable: false), fileName, null, cancellationToken);
        if (operational.Any(x => !x.Valido))
            throw new ArgumentException("A planilha contém linhas operacionais inválidas.");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var now = DateTime.UtcNow;
        var ignored = 0;
        var clientCount = 0;
        var paymentConditionCount = 0;
        var serviceCount = 0;
        var trackingCount = 0;
        var entryCount = 0;
        var costCount = 0;

        var clients = await db.Clientes.ToListAsync(cancellationToken);
        var services = await db.CadastrosServico.ToListAsync(cancellationToken);
        var paymentConditions = await db.CondicoesPagamento.ToListAsync(cancellationToken);
        var trackings = await db.Acompanhamentos.ToListAsync(cancellationToken);
        var entries = await db.Lancamentos.ToListAsync(cancellationToken);
        var costs = await db.CustosIndiretos.ToListAsync(cancellationToken);
        var clientByKey = clients
            .GroupBy(x => ClientKey(x.RazaoSocial, x.Telefone))
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var serviceByCode = services.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);
        var conditionByName = paymentConditions.ToDictionary(x => x.Nome, StringComparer.OrdinalIgnoreCase);
        var trackingByCode = trackings.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);

        foreach (var preview in operational)
        {
            var row = preview.Item!;
            var clientKey = ClientKey(row.Cliente, row.Telefone);
            if (!clientByKey.TryGetValue(clientKey, out var client))
            {
                client = new Cliente
                {
                    CnpjCpf = !string.IsNullOrWhiteSpace(row.CnpjCpf) ? CleanCnpjCpf(row.CnpjCpf) : LegacyDocument(clientKey),
                    RazaoSocial = Text(row.Cliente) ?? $"Cliente do serviço {row.Codigo}",
                    NomeContato = Text(row.Cliente),
                    Telefone = Text(row.Telefone),
                    Cidade = Text(row.Endereco)
                };
                db.Clientes.Add(client);
                clientByKey[clientKey] = client;
                clientCount++;
            }
            else if (!string.IsNullOrWhiteSpace(row.CnpjCpf) && client.CnpjCpf.StartsWith("LEG-", StringComparison.OrdinalIgnoreCase))
            {
                client.CnpjCpf = CleanCnpjCpf(row.CnpjCpf);
            }

            if (!serviceByCode.TryGetValue(row.Codigo, out var service))
            {
                CondicaoPagamento? paymentCondition = null;
                if (Text(row.CondicaoPagamento) is { } paymentName)
                {
                    if (!conditionByName.TryGetValue(paymentName, out paymentCondition))
                    {
                        var match = InstallmentRegex().Match(paymentName);
                        paymentCondition = new CondicaoPagamento
                        {
                            Nome = paymentName,
                            QuantidadeParcelas = match.Success && int.TryParse(match.Groups[1].Value, out var quantity)
                                ? Math.Clamp(quantity, 1, 120) : null,
                            IntervaloDias = 30,
                            Indefinido = !match.Success,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        db.CondicoesPagamento.Add(paymentCondition);
                        conditionByName[paymentName] = paymentCondition;
                        paymentConditionCount++;
                    }
                }
                service = new CadastroServico
                {
                    Codigo = row.Codigo,
                    Cliente = client,
                    CondicaoPagamento = paymentCondition,
                    TipoServico = row.Tipo,
                    Subtipo = Text(row.Servico) ?? row.Tipo.ToString(),
                    DataEntrada = row.DataContrato ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    SituacaoInicial = row.Situacao,
                    DocumentoEmpresa = client.CnpjCpf,
                    RazaoSocialEmpresa = client.RazaoSocial,
                    ContatoEmpresa = client.NomeContato,
                    Telefone = row.Telefone,
                    EnderecoEmpresa = row.Endereco,
                    EnderecoServico = row.Endereco,
                    MesmoEnderecoEmpresa = true,
                    ValorContrato = row.ValorContrato,
                    DataContrato = row.DataContrato,
                    NomeCondicaoPagamento = row.CondicaoPagamento,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                AddInstallments(service, row.ValorContrato, row.DataContrato, row.CondicaoPagamento);
                db.CadastrosServico.Add(service);
                serviceByCode[row.Codigo] = service;
                serviceCount++;
            }
            else ignored++;

            if (!trackingByCode.ContainsKey(row.Codigo))
            {
                var tracking = new AcompanhamentoServico
                {
                    OrigemId = row.OrigemId,
                    Codigo = row.Codigo,
                    TipoServico = row.Tipo,
                    NomeCliente = row.Cliente,
                    CnpjCpf = row.CnpjCpf?.Trim(),
                    Endereco = row.Endereco,
                    Telefone = row.Telefone,
                    Subtipo = row.Servico,
                    Situacao = row.Situacao,
                    Descricao = row.Descricao,
                    ValorContrato = row.ValorContrato,
                    DataContrato = row.DataContrato,
                    NotaFiscal = row.NotaFiscal,
                    CondicaoPagamento = row.CondicaoPagamento,
                    UltimaMudancaSituacaoEm = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                tracking.Historicos.Add(new AcompanhamentoServicoHistorico
                {
                    NovaSituacao = row.Situacao,
                    Descricao = "Importação integrada da planilha",
                    ResponsavelNome = "Sistema",
                    CreatedAt = now
                });
                db.Acompanhamentos.Add(tracking);
                trackingByCode[row.Codigo] = tracking;
                trackingCount++;
            }
            else ignored++;
        }

        using var workbook = new XLWorkbook(new MemoryStream(content, writable: false));
        if (workbook.TryGetWorksheet("LANÇAMENTOS", out var launchSheet))
            ImportEntries(launchSheet, serviceByCode, entries, now, ref entryCount, ref ignored);
        if (workbook.TryGetWorksheet("CUSTOS INDIRETOS", out var costSheet))
            ImportCosts(costSheet, costs, ref costCount, ref ignored);

        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return new(clientCount, paymentConditionCount, serviceCount, trackingCount, entryCount, costCount, ignored);
    }

    private void ImportEntries(
        IXLWorksheet sheet,
        IReadOnlyDictionary<string, CadastroServico> services,
        IReadOnlyCollection<Lancamento> existing,
        DateTime now,
        ref int created,
        ref int ignored)
    {
        var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var createdLocal = created;
        var ignoredLocal = ignored;
        for (var rowNumber = 2; rowNumber <= last; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            var serviceCode = CellText(row.Cell(1));
            var description = CellText(row.Cell(2));
            var date = CellDate(row.Cell(4));
            if (string.IsNullOrWhiteSpace(serviceCode) || string.IsNullOrWhiteSpace(description) || date is null)
            {
                ignoredLocal++;
                continue;
            }

            services.TryGetValue(serviceCode, out var service);
            AddEntry("E", row.Cell(3), LancamentoTipo.ENTRADA);
            AddEntry("S", row.Cell(5), LancamentoTipo.SAIDA);

            void AddEntry(string suffix, IXLCell valueCell, LancamentoTipo type)
            {
                var value = CellDecimal(valueCell);
                if (value is null or <= 0) return;
                var importCode = $"IMP-L-{rowNumber:000000}-{suffix}";
                if (existing.Any(x => x.Codigo.Equals(importCode, StringComparison.OrdinalIgnoreCase))
                    || db.Lancamentos.Local.Any(x => x.Codigo.Equals(importCode, StringComparison.OrdinalIgnoreCase)))
                {
                    ignoredLocal++;
                    return;
                }
                db.Lancamentos.Add(new Lancamento
                {
                    Codigo = importCode,
                    Tipo = type,
                    Status = LancamentoStatus.PAGO,
                    Origem = LancamentoOrigem.IMPORT_ATLAS,
                    CadastroServico = service,
                    CodigoServico = serviceCode,
                    NomeCliente = service?.RazaoSocialEmpresa,
                    Descricao = description,
                    Valor = value,
                    Data = date,
                    Observacao = CellText(row.Cell(7)),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                createdLocal++;
            }
        }
        created = createdLocal;
        ignored = ignoredLocal;
    }

    private void ImportCosts(
        IXLWorksheet sheet,
        IReadOnlyCollection<CustoIndireto> existing,
        ref int created,
        ref int ignored)
    {
        var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 2; rowNumber <= last; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            var date = CellDate(row.Cell(1));
            var description = CellText(row.Cell(2));
            var value = CellDecimal(row.Cell(3));
            var category = CellText(row.Cell(4));
            if (date is null || string.IsNullOrWhiteSpace(description) || value is null or <= 0)
            {
                ignored++;
                continue;
            }
            category = Text(category) ?? "Não informado";
            if (existing.Any(x => x.Data == date && x.Valor == value
                && x.Descricao.Equals(description, StringComparison.OrdinalIgnoreCase)
                && x.Categoria.Equals(category, StringComparison.OrdinalIgnoreCase))
                || db.CustosIndiretos.Local.Any(x => x.Data == date && x.Valor == value
                && x.Descricao.Equals(description, StringComparison.OrdinalIgnoreCase)
                && x.Categoria.Equals(category, StringComparison.OrdinalIgnoreCase)))
            {
                ignored++;
                continue;
            }
            db.CustosIndiretos.Add(new CustoIndireto
            {
                Data = date.Value,
                Descricao = description,
                Valor = value.Value,
                Categoria = category
            });
            created++;
        }
    }

    private static void AddInstallments(
        CadastroServico service, decimal? contractValue, DateOnly? contractDate, string? condition)
    {
        if (contractValue is null or <= 0) return;
        var match = InstallmentRegex().Match(condition ?? "");
        var count = match.Success && int.TryParse(match.Groups[1].Value, out var parsed)
            ? Math.Clamp(parsed, 1, 120) : 1;
        var baseValue = decimal.Floor(contractValue.Value / count * 100) / 100;
        for (var index = 1; index <= count; index++)
            service.Parcelas.Add(new CadastroServicoParcela
            {
                NumeroParcela = index,
                Valor = index == count ? contractValue.Value - baseValue * (count - 1) : baseValue,
                DataVencimento = contractDate?.AddMonths(index - 1),
                FormaPagamento = condition
            });
    }

    private static string LegacyDocument(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"LEG-{Convert.ToHexString(hash)[..12]}";
    }

    private static string CleanCnpjCpf(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return digits.Length switch
        {
            11 => Convert.ToUInt64(digits).ToString(@"000\.000\.000\-00"),
            14 => Convert.ToUInt64(digits).ToString(@"00\.000\.000\/0000\-00"),
            _ => input.Trim()
        };
    }

    private static string ClientKey(string? name, string? phone) =>
        $"{Text(name)?.ToUpperInvariant() ?? "SEM NOME"}|{Digits(phone)}";

    private static string Digits(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());
    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string CellText(IXLCell cell) => cell.IsEmpty() ? "" : cell.GetFormattedString().Trim();
    private static decimal? CellDecimal(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<decimal>(out var number)) return number;
        var text = cell.GetFormattedString().Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(text, NumberStyles.Number, PtBr, out number)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number) ? number : null;
    }
    private static DateOnly? CellDate(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var date)) return DateOnly.FromDateTime(date);
        if (cell.TryGetValue<double>(out var serial)) return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        return DateOnly.TryParse(cell.GetFormattedString(), PtBr, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"(\d+)\s*x", RegexOptions.IgnoreCase)]
    private static partial Regex InstallmentRegex();
}
