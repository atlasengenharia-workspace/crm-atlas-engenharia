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

public sealed partial class AtlasWorkbookImportService(AtlasDbContext db) : IAtlasWorkbookImportService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<AtlasWorkbookImportResult> ImportAsync(
        Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".xlsb", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A importação integrada requer um arquivo .xlsx ou .xlsb.");

        await using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes, cancellationToken);
        var content = bytes.ToArray();

        // O arquivo e convertido uma unica vez. Antes o workbook era montado
        // duas vezes (leitor operacional + abas financeiras), o que dobrava o
        // custo de memoria de uma planilha grande.
        using var workbook = SpreadsheetWorkbookLoader.Load(
            new MemoryStream(content, writable: false), fileName);

        var operational = AcompanhamentoSpreadsheetReader.ReadWorkbook(workbook, null);
        var validOperational = operational.Where(x => x.Valido && x.Item != null).ToList();
        if (validOperational.Count == 0)
            throw new ArgumentException("A planilha não contém nenhuma linha operacional válida.");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var now = DateTime.UtcNow;
        var ignored = operational.Count(x => !x.Valido);
        var clientCount = 0;
        var paymentConditionCount = 0;
        var serviceCount = 0;
        var trackingCount = 0;
        var entryCount = 0;
        var costCount = 0;

        var clients = await db.Clientes.ToListAsync(cancellationToken);
        var services = await db.CadastrosServico
            .Include(x => x.Parcelas)
            .ToListAsync(cancellationToken);
        var paymentConditions = await db.CondicoesPagamento.ToListAsync(cancellationToken);
        var trackings = await db.Acompanhamentos.ToListAsync(cancellationToken);
        var entries = await db.Lancamentos.ToListAsync(cancellationToken);
        var costs = await db.CustosIndiretos.ToListAsync(cancellationToken);

        var clientByKey = clients
            .GroupBy(x => ClientKey(x.RazaoSocial, x.Telefone))
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        // Segundo indice, por documento. O indice unico IX_clientes_cnpj_cpf
        // e a fonte da verdade no banco: se o CNPJ/CPF (ou o documento LEG-
        // gerado para cliente sem documento) ja existe, e o mesmo cliente,
        // ainda que nome ou telefone tenham mudado desde a ultima importacao.
        var clientByDocument = clients
            .Where(x => !string.IsNullOrWhiteSpace(x.CnpjCpf))
            .GroupBy(x => x.CnpjCpf, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var serviceByCode = services
            .GroupBy(x => CodeKey(x.Codigo), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var conditionByName = paymentConditions.ToDictionary(x => x.Nome, StringComparer.OrdinalIgnoreCase);
        var trackingByCode = trackings
            .GroupBy(x => CodeKey(x.Codigo), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var preview in validOperational)
        {
            var row = preview.Item!;
            var clientKey = ClientKey(row.Cliente, row.Telefone);

            // 1. Process / Update Client
            if (!clientByKey.TryGetValue(clientKey, out var client))
            {
                var document = !string.IsNullOrWhiteSpace(row.CnpjCpf)
                    ? CleanCnpjCpf(row.CnpjCpf)
                    : LegacyDocument(clientKey);

                // Linhas sem nome de cliente eram gravadas com RazaoSocial
                // "Cliente do serviço X". Na reimportacao a chave nome|telefone
                // deixava de bater, um segundo cliente era criado com o mesmo
                // documento LEG- e o banco recusava com IX_clientes_cnpj_cpf.
                // Procurar pelo documento antes de inserir resolve esse caso e
                // qualquer outro em que o nome tenha sido editado no CRM.
                if (string.IsNullOrWhiteSpace(document)
                    || !clientByDocument.TryGetValue(document, out client))
                {
                    client = new Cliente
                    {
                        CnpjCpf = document,
                        RazaoSocial = Text(row.Cliente) ?? $"Cliente do serviço {row.Codigo}",
                        NomeContato = Text(row.Cliente),
                        Telefone = Text(row.Telefone),
                        Cidade = Text(row.Endereco)
                    };
                    db.Clientes.Add(client);
                    if (!string.IsNullOrWhiteSpace(document)) clientByDocument[document] = client;
                    clientCount++;
                }
                clientByKey[clientKey] = client;
            }
            else if (!string.IsNullOrWhiteSpace(row.CnpjCpf))
            {
                TrySetDocument(client, CleanCnpjCpf(row.CnpjCpf), clientByDocument);
            }
            if (!string.IsNullOrWhiteSpace(row.Telefone)) client.Telefone = Text(row.Telefone);
            if (!string.IsNullOrWhiteSpace(row.Endereco)) client.Cidade = Text(row.Endereco);

            // 2. Process Payment Condition
            if (Text(row.CondicaoPagamento) is { } paymentName && !conditionByName.ContainsKey(paymentName))
            {
                var match = InstallmentRegex().Match(paymentName);
                var paymentCondition = new CondicaoPagamento
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

            // 3. Process / Update Service
            if (!serviceByCode.TryGetValue(CodeKey(row.Codigo), out var service))
            {
                conditionByName.TryGetValue(Text(row.CondicaoPagamento) ?? "", out var paymentCondition);
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
                serviceByCode[CodeKey(row.Codigo)] = service;
                serviceCount++;
            }
            else
            {
                conditionByName.TryGetValue(Text(row.CondicaoPagamento) ?? "", out var paymentCondition);
                service.Cliente = client;
                service.CondicaoPagamento = paymentCondition;
                service.TipoServico = row.Tipo;
                service.Subtipo = Text(row.Servico) ?? row.Tipo.ToString();
                service.DataEntrada = row.DataContrato ?? service.DataEntrada;
                service.SituacaoInicial = row.Situacao;
                service.DocumentoEmpresa = client.CnpjCpf;
                service.RazaoSocialEmpresa = client.RazaoSocial;
                service.ContatoEmpresa = client.NomeContato;
                service.Telefone = row.Telefone;
                service.EnderecoEmpresa = row.Endereco;
                service.EnderecoServico = row.Endereco;
                service.ValorContrato = row.ValorContrato;
                service.DataContrato = row.DataContrato;
                service.NomeCondicaoPagamento = row.CondicaoPagamento;
                if (service.Parcelas.Count == 0)
                    AddInstallments(service, row.ValorContrato, row.DataContrato, row.CondicaoPagamento);
                service.UpdatedAt = now;
            }

            // Na planilha a coluna "Prox. Parcela" existe so na aba de
            // Processos Adm e guarda anotacao de tarefa ("Finalizar",
            // "Protocolar"), nunca uma data — por isso a coluna chegava sempre
            // vazia no CRM. Quando a planilha nao traz data, deduzimos o
            // proximo vencimento pelo parcelamento gerado do contrato.
            // A dedução só entra quando a planilha não trouxe nada na coluna —
            // nem data, nem anotação. Assim a coluna nunca fica ambígua: ou
            // mostra o que está na planilha, ou o vencimento em aberto.
            var proximaParcela = row.ProximaParcela
                ?? (string.IsNullOrWhiteSpace(row.ProximaParcelaTexto)
                    ? NextInstallmentDate(service, row.Recebido)
                    : null);

            // 4. Process / Update Tracking
            if (!trackingByCode.TryGetValue(CodeKey(row.Codigo), out var tracking))
            {
                tracking = new AcompanhamentoServico
                {
                    OrigemId = row.OrigemId,
                    Codigo = row.Codigo,
                    TipoServico = row.Tipo,
                    NomeCliente = row.Cliente,
                    CnpjCpf = !string.IsNullOrWhiteSpace(row.CnpjCpf) ? CleanCnpjCpf(row.CnpjCpf) : client.CnpjCpf,
                    Endereco = row.Endereco,
                    Telefone = row.Telefone,
                    Subtipo = row.Servico,
                    Situacao = row.Situacao,
                    Descricao = row.Descricao,
                    ValorContrato = row.ValorContrato,
                    DataContrato = row.DataContrato,
                    NotaFiscal = row.NotaFiscal,
                    CondicaoPagamento = row.CondicaoPagamento,
                    AReceber = row.AReceber,
                    Recebido = row.Recebido,
                    Custos = row.Custos,
                    ProximaParcela = proximaParcela,
                    ProximaParcelaTexto = row.ProximaParcelaTexto,
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
                trackingByCode[CodeKey(row.Codigo)] = tracking;
                trackingCount++;
            }
            else
            {
                var statusChanged = !string.Equals(tracking.Situacao, row.Situacao, StringComparison.OrdinalIgnoreCase);
                if (statusChanged)
                    tracking.Historicos.Add(new AcompanhamentoServicoHistorico
                    {
                        SituacaoAnterior = tracking.Situacao,
                        NovaSituacao = row.Situacao,
                        Descricao = "Atualização pela planilha integrada",
                        ResponsavelNome = "Sistema",
                        CreatedAt = now
                    });
                tracking.TipoServico = row.Tipo;
                tracking.NomeCliente = row.Cliente;
                tracking.CnpjCpf = !string.IsNullOrWhiteSpace(row.CnpjCpf) ? CleanCnpjCpf(row.CnpjCpf) : client.CnpjCpf;
                tracking.Endereco = row.Endereco;
                tracking.Telefone = row.Telefone;
                tracking.Subtipo = row.Servico;
                tracking.Situacao = row.Situacao;
                tracking.Descricao = row.Descricao;
                tracking.ValorContrato = row.ValorContrato;
                tracking.DataContrato = row.DataContrato;
                tracking.NotaFiscal = row.NotaFiscal;
                tracking.CondicaoPagamento = row.CondicaoPagamento;
                tracking.AReceber = row.AReceber;
                tracking.Recebido = row.Recebido;
                tracking.Custos = row.Custos;
                tracking.ProximaParcela = proximaParcela;
                tracking.ProximaParcelaTexto = row.ProximaParcelaTexto;
                if (statusChanged) tracking.UltimaMudancaSituacaoEm = now;
                tracking.UpdatedAt = now;
            }
        }

        if (FindWorksheet(workbook, "condicoespagamento", "condicaopagamento", "condicoes", "formaspagamento") is { } condSheet)
            ImportPaymentConditions(condSheet, conditionByName, now, ref paymentConditionCount);

        if (FindWorksheet(workbook, "clientes", "cliente", "empresas", "pessoas") is { } clientSheet)
            ImportClientsSheet(clientSheet, clientByKey, clientByDocument, ref clientCount);

        if (FindWorksheet(workbook, "lancamentos", "lancamento", "entradas", "financeiro", "caixa") is { } launchSheet)
            ImportEntries(launchSheet, serviceByCode, entries, now, ref entryCount, ref ignored);

        if (FindWorksheet(workbook, "custosindiretos", "custos", "despesas", "custoindireto", "gastos") is { } costSheet)
            ImportCosts(costSheet, costs, ref costCount, ref ignored);

        EnsureUniqueClientDocuments();
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new(clientCount, paymentConditionCount, serviceCount, trackingCount, entryCount, costCount, ignored);
    }

    private static IXLWorksheet? FindWorksheet(XLWorkbook workbook, params string[] candidateNames)
    {
        foreach (var sheet in workbook.Worksheets)
        {
            var normalized = NormalizeText(sheet.Name);
            foreach (var candidate in candidateNames)
            {
                var normCandidate = NormalizeText(candidate);
                if (normalized.Equals(normCandidate, StringComparison.OrdinalIgnoreCase) || normalized.Contains(normCandidate, StringComparison.OrdinalIgnoreCase))
                {
                    return sheet;
                }
            }
        }
        return null;
    }

    private void ImportPaymentConditions(
        IXLWorksheet sheet,
        IDictionary<string, CondicaoPagamento> conditionByName,
        DateTime now,
        ref int created)
    {
        var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var headerRow = FindHeaderRowInSheet(sheet, ["nome", "condicao"]);
        var headers = headerRow != null ? BuildHeaderMapFromRow(headerRow) : new Dictionary<string, int>();
        var startRow = (headerRow?.RowNumber() ?? 1) + 1;

        for (var rowNumber = startRow; rowNumber <= last; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (row.IsEmpty()) continue;
            var name = GetCellText(row, headers, ["nome", "condicao", "condicaopagamento"]) ?? CellText(row.Cell(1));
            if (string.IsNullOrWhiteSpace(name)) continue;

            name = name.Trim();
            if (!conditionByName.ContainsKey(name))
            {
                var match = InstallmentRegex().Match(name);
                var installmentsStr = GetCellText(row, headers, ["parcelas", "quantidade", "quantidadeparcelas"]);
                var installments = int.TryParse(installmentsStr, out var q) ? q : (match.Success && int.TryParse(match.Groups[1].Value, out q) ? q : (int?)null);

                var condition = new CondicaoPagamento
                {
                    Nome = name,
                    QuantidadeParcelas = installments.HasValue ? Math.Clamp(installments.Value, 1, 120) : null,
                    IntervaloDias = 30,
                    Indefinido = !installments.HasValue,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.CondicoesPagamento.Add(condition);
                conditionByName[name] = condition;
                created++;
            }
        }
    }

    private void ImportClientsSheet(
        IXLWorksheet sheet,
        IDictionary<string, Cliente> clientByKey,
        IDictionary<string, Cliente> clientByDocument,
        ref int created)
    {
        var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var headerRow = FindHeaderRowInSheet(sheet, ["cliente", "nome", "cnpj"]);
        var headers = headerRow != null ? BuildHeaderMapFromRow(headerRow) : new Dictionary<string, int>();
        var startRow = (headerRow?.RowNumber() ?? 1) + 1;

        for (var rowNumber = startRow; rowNumber <= last; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (row.IsEmpty()) continue;

            var name = GetCellText(row, headers, ["cliente", "nome", "razaosocial", "empresa"]) ?? CellText(row.Cell(1));
            var cnpjCpf = GetCellText(row, headers, ["cnpjcpf", "cpf", "cnpj", "documento", "doc"]) ?? CellText(row.Cell(2));
            var phone = GetCellText(row, headers, ["telefone", "celular", "fone", "contato"]) ?? CellText(row.Cell(3));
            var address = GetCellText(row, headers, ["endereco", "cidade", "localidade"]) ?? CellText(row.Cell(4));

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(cnpjCpf)) continue;

            var key = ClientKey(name, phone);
            if (!clientByKey.TryGetValue(key, out var client))
            {
                var document = !string.IsNullOrWhiteSpace(cnpjCpf) ? CleanCnpjCpf(cnpjCpf) : LegacyDocument(key);
                if (string.IsNullOrWhiteSpace(document)
                    || !clientByDocument.TryGetValue(document, out client))
                {
                    client = new Cliente
                    {
                        CnpjCpf = document,
                        RazaoSocial = Text(name) ?? "Cliente sem nome",
                        NomeContato = Text(name),
                        Telefone = Text(phone),
                        Cidade = Text(address)
                    };
                    db.Clientes.Add(client);
                    if (!string.IsNullOrWhiteSpace(document)) clientByDocument[document] = client;
                    created++;
                }
                clientByKey[key] = client;
            }
            else if (!string.IsNullOrWhiteSpace(cnpjCpf))
            {
                TrySetDocument(client, CleanCnpjCpf(cnpjCpf), clientByDocument);
            }
        }
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
        var headerRow = FindHeaderRowInSheet(sheet, ["codigo", "descricao"]);
        var headers = headerRow != null ? BuildHeaderMapFromRow(headerRow) : new Dictionary<string, int>();
        var startRow = (headerRow?.RowNumber() ?? 1) + 1;

        var createdLocal = created;
        var ignoredLocal = ignored;

        // Antes a checagem de duplicidade varria a lista inteira por linha
        // (O(n^2)); com ~2.000 lancamentos isso dominava o tempo de importacao.
        var knownCodes = new HashSet<string>(existing.Select(x => x.Codigo), StringComparer.OrdinalIgnoreCase);
        foreach (var local in db.Lancamentos.Local) knownCodes.Add(local.Codigo);

        for (var rowNumber = startRow; rowNumber <= last; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (row.IsEmpty()) continue;

            var serviceCode = GetCellText(row, headers, ["codigo", "codigoservico", "servico", "os"]) ?? CellText(row.Cell(1));
            var description = GetCellText(row, headers, ["descricao", "historico", "item"]) ?? CellText(row.Cell(2));
            var date = GetCellDate(row, headers, ["data", "dataemissao", "vencimento"]) ?? CellDate(row.Cell(4));

            services.TryGetValue(CodeKey(serviceCode), out var service);
            var entryValueCell = GetCellByHeaderNames(row, headers, ["entrada", "receita", "faturamento", "credito"]) ?? row.Cell(3);
            var exitValueCell = GetCellByHeaderNames(row, headers, ["saida", "despesa", "custodireto", "debito"]) ?? row.Cell(5);
            var obsCell = GetCellByHeaderNames(row, headers, ["observacao", "obs", "nota"]) ?? row.Cell(7);

            var entryValue = CellDecimal(entryValueCell);
            var exitValue = CellDecimal(exitValueCell);

            // So descartamos a linha quando nao ha data ou nao ha valor algum.
            // Descricao em branco nao e motivo para perder um lancamento de
            // dinheiro: a planilha tem linhas com valor e sem texto.
            if (date is null || (entryValue is null or <= 0 && exitValue is null or <= 0))
            {
                ignoredLocal++;
                continue;
            }
            if (string.IsNullOrWhiteSpace(description))
                description = string.IsNullOrWhiteSpace(serviceCode)
                    ? $"Lançamento importado (linha {rowNumber})"
                    : $"Lançamento do serviço {serviceCode.Trim()}";

            AddEntry("E", entryValue, LancamentoTipo.ENTRADA, CellText(obsCell));
            AddEntry("S", exitValue, LancamentoTipo.SAIDA, CellText(obsCell));

            void AddEntry(string suffix, decimal? value, LancamentoTipo type, string obs)
            {
                if (value is null or <= 0) return;
                var importCode = $"IMP-L-{rowNumber:000000}-{suffix}";
                if (!knownCodes.Add(importCode))
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
                    Observacao = obs,
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
        var headerRow = FindHeaderRowInSheet(sheet, ["data", "descricao", "valor"]);
        var headers = headerRow != null ? BuildHeaderMapFromRow(headerRow) : new Dictionary<string, int>();
        var startRow = (headerRow?.RowNumber() ?? 1) + 1;

        // A planilha repete legitimamente a mesma despesa no mesmo dia (mesma
        // data, descricao, valor e categoria). A checagem antiga descartava
        // essas repeticoes como duplicidade e perdia dinheiro real. Agora
        // contamos quantas vezes cada combinacao ja existe no banco e so
        // ignoramos o excedente — a reimportacao continua sendo idempotente.
        var alreadyStored = new Dictionary<(DateOnly, decimal, string, string), int>();
        foreach (var cost in existing)
        {
            var key = CustoKey(cost.Data, cost.Valor, cost.Descricao, cost.Categoria);
            alreadyStored[key] = alreadyStored.GetValueOrDefault(key) + 1;
        }

        for (var rowNumber = startRow; rowNumber <= last; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (row.IsEmpty()) continue;

            DateOnly? date = GetCellDate(row, headers, ["data", "dataemissao", "vencimento"]) ?? CellDate(row.Cell(1));
            string? description = GetCellText(row, headers, ["descricao", "item", "historico", "nome"]) ?? CellText(row.Cell(2));
            decimal? value = GetCellDecimal(row, headers, ["valor", "custo", "total", "quantia"]) ?? CellDecimal(row.Cell(3));
            string? category = GetCellText(row, headers, ["categoria", "grupo", "tipo", "centro"]) ?? CellText(row.Cell(4));

            if (date is null || string.IsNullOrWhiteSpace(description) || value is null or <= 0)
            {
                ignored++;
                continue;
            }
            category = Text(category) ?? "Não informado";
            description = description.Trim();

            var rowKey = CustoKey(date.Value, value.Value, description, category);
            var pending = alreadyStored.GetValueOrDefault(rowKey);
            if (pending > 0)
            {
                alreadyStored[rowKey] = pending - 1;
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

    private static (DateOnly, decimal, string, string) CustoKey(
        DateOnly data, decimal valor, string descricao, string categoria) =>
        (data, valor, descricao.Trim().ToUpperInvariant(), categoria.Trim().ToUpperInvariant());

    private static IXLRow? FindHeaderRowInSheet(IXLWorksheet sheet, string[] requiredKeywords)
    {
        var last = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 0, 15);
        for (var rowNumber = 1; rowNumber <= last; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            var headerText = string.Join(" ", row.CellsUsed().Select(c => NormalizeText(c.GetString())));
            if (requiredKeywords.Any(k => headerText.Contains(NormalizeText(k), StringComparison.OrdinalIgnoreCase)))
            {
                return row;
            }
        }
        return null;
    }

    private static Dictionary<string, int> BuildHeaderMapFromRow(IXLRow row) =>
        row.CellsUsed().Select(cell => (Name: NormalizeText(cell.GetString()), Index: cell.Address.ColumnNumber))
            .Where(x => x.Name.Length > 0)
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);

    private static IXLCell? GetCellByHeaderNames(IXLRow row, IReadOnlyDictionary<string, int> headers, string[] candidateNames)
    {
        foreach (var candidate in candidateNames)
        {
            var norm = NormalizeText(candidate);
            foreach (var kvp in headers)
            {
                if (kvp.Key.Equals(norm, StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains(norm, StringComparison.OrdinalIgnoreCase))
                {
                    return row.Cell(kvp.Value);
                }
            }
        }
        return null;
    }

    private static string? GetCellText(IXLRow row, IReadOnlyDictionary<string, int> headers, string[] candidateNames)
    {
        var cell = GetCellByHeaderNames(row, headers, candidateNames);
        return cell != null ? CellText(cell) : null;
    }

    private static decimal? GetCellDecimal(IXLRow row, IReadOnlyDictionary<string, int> headers, string[] candidateNames)
    {
        var cell = GetCellByHeaderNames(row, headers, candidateNames);
        return cell != null ? CellDecimal(cell) : null;
    }

    private static DateOnly? GetCellDate(IXLRow row, IReadOnlyDictionary<string, int> headers, string[] candidateNames)
    {
        var cell = GetCellByHeaderNames(row, headers, candidateNames);
        return cell != null ? CellDate(cell) : null;
    }

    // Primeira parcela cujo acumulado ainda nao foi coberto pelo valor
    // recebido — ou seja, o proximo vencimento em aberto. Devolve null quando
    // o contrato ja esta quitado ou nao tem parcelamento.
    private static DateOnly? NextInstallmentDate(CadastroServico service, decimal? received)
    {
        var paid = received ?? 0m;
        var installments = service.Parcelas
            .Where(x => x.DataVencimento.HasValue)
            .OrderBy(x => x.NumeroParcela ?? int.MaxValue)
            .ThenBy(x => x.DataVencimento!.Value)
            .ToList();

        var accumulated = 0m;
        foreach (var installment in installments)
        {
            accumulated += installment.Valor ?? 0m;
            if (accumulated > paid) return installment.DataVencimento;
        }
        return null;
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

    // Rede de seguranca antes do SaveChanges. O PostgreSQL rejeita a colisao
    // com "23505 IX_clientes_cnpj_cpf" e esconde o valor duplicado, o que torna
    // o diagnostico impossivel. Aqui a comparacao e Ordinal, igual a do indice,
    // e a mensagem diz exatamente quais clientes colidiram.
    private void EnsureUniqueClientDocuments()
    {
        var seen = new Dictionary<string, Cliente>(StringComparer.Ordinal);
        foreach (var entry in db.ChangeTracker.Entries<Cliente>())
        {
            if (entry.State is EntityState.Deleted or EntityState.Detached) continue;
            var document = entry.Entity.CnpjCpf;
            if (string.IsNullOrWhiteSpace(document)) continue;
            if (seen.TryGetValue(document, out var other))
                throw new InvalidOperationException(
                    $"A importação geraria dois clientes com o mesmo CNPJ/CPF \"{document}\": " +
                    $"\"{other.RazaoSocial}\" (id {other.Id}) e \"{entry.Entity.RazaoSocial}\" (id {entry.Entity.Id}). " +
                    "Corrija o documento de um deles no cadastro de clientes e importe novamente.");
            seen[document] = entry.Entity;
        }
    }

    // So promove o documento do cliente quando ninguem mais esta usando aquele
    // CNPJ/CPF. Sobrescrever as cegas quebrava o indice unico do banco quando a
    // planilha trazia o mesmo documento em dois clientes diferentes.
    private static void TrySetDocument(
        Cliente client, string document, IDictionary<string, Cliente> clientByDocument)
    {
        if (string.IsNullOrWhiteSpace(document)) return;
        if (!string.IsNullOrWhiteSpace(client.CnpjCpf)
            && !client.CnpjCpf.StartsWith("LEG-", StringComparison.OrdinalIgnoreCase)) return;
        if (clientByDocument.TryGetValue(document, out var owner) && !ReferenceEquals(owner, client)) return;

        if (!string.IsNullOrWhiteSpace(client.CnpjCpf)) clientByDocument.Remove(client.CnpjCpf);
        client.CnpjCpf = document;
        clientByDocument[document] = client;
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

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
                builder.Append(character);
        return builder.ToString();
    }

    private static string ClientKey(string? name, string? phone) =>
        $"{Text(name)?.ToUpperInvariant() ?? "SEM NOME"}|{Digits(phone)}";

    private static string CodeKey(string? value)
    {
        var text = Text(value) ?? string.Empty;
        text = text.Replace("\u00A0", " ", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        if (text.Contains(',', StringComparison.Ordinal) && !text.Contains('.', StringComparison.Ordinal))
            text = text.Replace(',', '.');
        return text.ToUpperInvariant();
    }

    private static string Digits(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());
    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string CellText(IXLCell cell) => cell.IsEmpty() ? "" : cell.GetFormattedString().Trim();
    private static decimal? CellDecimal(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<decimal>(out var number)) return number;
        var text = cell.GetFormattedString().Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
        var comma = text.LastIndexOf(',');
        var dot = text.LastIndexOf('.');
        var culture = dot >= 0 && comma < 0 && text.Length - dot is 2 or 3
            ? CultureInfo.InvariantCulture
            : comma >= 0 && dot < 0
                ? PtBr
                : dot > comma ? CultureInfo.InvariantCulture : PtBr;
        return decimal.TryParse(text, NumberStyles.Number, culture, out number) ? number : null;
    }
    private static DateOnly? CellDate(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var date)) return DateOnly.FromDateTime(date);
        if (cell.TryGetValue<double>(out var serial) && serial is > 0 and < 2958466)
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(serial)); } catch { }
        }
        var formatted = cell.GetFormattedString();
        if (DateTime.TryParse(formatted, PtBr, DateTimeStyles.None, out var dt)
            || DateTime.TryParse(formatted, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            return DateOnly.FromDateTime(dt);
        }
        return DateOnly.TryParse(formatted, PtBr, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"(\d+)\s*x", RegexOptions.IgnoreCase)]
    private static partial Regex InstallmentRegex();
}
