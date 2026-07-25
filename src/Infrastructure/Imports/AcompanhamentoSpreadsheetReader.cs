using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Operacao;

namespace CrmAtlas.Infrastructure.Imports;

public sealed partial class AcompanhamentoSpreadsheetReader : IAcompanhamentoSpreadsheetReader
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] RequiredHeaders = ["codigo", "nomecliente", "situacao"];

    public async Task<IReadOnlyList<AcompanhamentoImportPreviewDto>> ReadAsync(
        Stream stream, string fileName, AcompanhamentoServicoTipo? tipo = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("O nome do arquivo é obrigatório.", nameof(fileName));

        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            await using var bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream, ct);
            bufferedStream.Position = 0;
            return ReadWorkbook(bufferedStream, tipo);
        }
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return await ReadCsvAsync(stream, fileName, tipo, ct);
        throw new ArgumentException("Formato não suportado. Selecione um arquivo .xlsx ou .csv.");
    }

    private static IReadOnlyList<AcompanhamentoImportPreviewDto> ReadWorkbook(Stream stream, AcompanhamentoServicoTipo? requestedType)
    {
        using var workbook = new XLWorkbook(stream);
        var result = new List<AcompanhamentoImportPreviewDto>();
        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetType = ResolveType(worksheet.Name, requestedType);
            if (sheetType is null || requestedType is not null && sheetType != requestedType) continue;
            var headerRow = FindHeaderRow(worksheet);
            if (headerRow is null) continue;
            var headers = BuildHeaderMap(headerRow);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
            for (var rowNumber = headerRow.RowNumber() + 1; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (!row.IsEmpty())
                    result.Add(ParseRow(rowNumber, worksheet.Name, sheetType.Value, header => ReadCell(row, headers, header)));
            }
        }
        return result.Count > 0 ? result : throw new ArgumentException("Nenhuma aba compatível com acompanhamento foi encontrada no arquivo.");
    }

    private static async Task<IReadOnlyList<AcompanhamentoImportPreviewDto>> ReadCsvAsync(
        Stream stream, string fileName, AcompanhamentoServicoTipo? requestedType, CancellationToken ct)
    {
        var type = ResolveType(Path.GetFileNameWithoutExtension(fileName), requestedType)
            ?? throw new ArgumentException("Não foi possível identificar o tipo. Importe pela página AVCB, CLCB, Obras ou Processos.");
        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
        var result = new List<AcompanhamentoImportPreviewDto>();
        Dictionary<string, int>? headers = null;
        var lineNumber = 0;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = ParseCsvLine(line);
            if (headers is null)
            {
                headers = BuildHeaderMap(fields);
                EnsureHeaders(headers);
                continue;
            }
            result.Add(ParseRow(lineNumber, Path.GetFileName(fileName), type,
                header => ReadField(fields, headers, header)));
        }
        return result.Count > 0 ? result : throw new ArgumentException("O arquivo não contém linhas de dados.");
    }

    private static AcompanhamentoImportPreviewDto ParseRow(
        int line, string sheet, AcompanhamentoServicoTipo type, Func<string, string?> value)
    {
        var code = value("codigo")?.Trim() ?? "";
        var client = value("nomecliente")?.Trim();
        var cnpjCpf = value("cnpjcpf")?.Trim();
        var address = value("endereco")?.Trim();
        var phone = value("telefone")?.Trim();
        var service = value("servico")?.Trim();
        var status = value("situacao")?.Trim();
        if (string.IsNullOrWhiteSpace(status))
            status = "Não informado";
        var description = value("descricaosituacao")?.Trim();
        var invoice = value("nf")?.Trim();
        var paymentTerms = value("condicaopagamento")?.Trim();
        var valueOk = TryDecimal(value("contrato"), out var contract);
        var dateOk = TryDate(value("datacontrato"), out var contractDate);
        var error = string.IsNullOrWhiteSpace(code) ? "Código obrigatório"
            : !valueOk ? "Valor do contrato inválido"
            : !dateOk ? "Data do contrato inválida" : null;
        var dto = error is null
            ? new AcompanhamentoImportDto(CreateOriginId(code), code, type, client, address, phone, service,
                status, description, contract, contractDate, invoice, paymentTerms, cnpjCpf)
            : null;
        return new(line, sheet, code, type.ToString(), client, error is null, error, dto);
    }

    private static IXLRow? FindHeaderRow(IXLWorksheet worksheet)
    {
        var last = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 0, 30);
        for (var rowNumber = 1; rowNumber <= last; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var headers = BuildHeaderMap(row);
            if (RequiredHeaders.All(headers.ContainsKey))
            {
                EnsureHeaders(headers);
                return row;
            }
        }
        return null;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRow row) =>
        row.CellsUsed().Select(cell => (Name: NormalizeHeader(cell.GetString()), Index: cell.Address.ColumnNumber))
            .Where(x => x.Name.Length > 0).GroupBy(x => CanonicalHeader(x.Name))
            .ToDictionary(x => x.Key, x => x.First().Index);

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> fields) =>
        fields.Select((value, index) => (Name: NormalizeHeader(value), Index: index))
            .Where(x => x.Name.Length > 0).GroupBy(x => CanonicalHeader(x.Name))
            .ToDictionary(x => x.Key, x => x.First().Index);

    private static void EnsureHeaders(IReadOnlyDictionary<string, int> headers)
    {
        var missing = RequiredHeaders.Where(x => !headers.ContainsKey(x)).ToArray();
        if (missing.Length > 0) throw new ArgumentException($"Colunas obrigatórias ausentes: {string.Join(", ", missing)}.");
    }

    private static string? ReadCell(IXLRow row, IReadOnlyDictionary<string, int> headers, string header)
    {
        if (!headers.TryGetValue(header, out var column)) return null;
        var cell = row.Cell(column);
        if (cell.IsEmpty()) return null;
        if (header == "datacontrato" && cell.TryGetValue<DateTime>(out var date))
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (cell.DataType == XLDataType.Number) return cell.GetDouble().ToString(CultureInfo.InvariantCulture);
        return cell.GetFormattedString();
    }

    private static string? ReadField(IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> headers, string header) =>
        headers.TryGetValue(header, out var index) && index < fields.Count ? fields[index] : null;

    private static AcompanhamentoServicoTipo? ResolveType(string name, AcompanhamentoServicoTipo? fallback)
    {
        var normalized = NormalizeHeader(name);
        if (normalized.Contains("avcb")) return AcompanhamentoServicoTipo.AVCB;
        if (normalized.Contains("clcb")) return AcompanhamentoServicoTipo.CLCB;
        if (normalized.Contains("obra")) return AcompanhamentoServicoTipo.OBRAS;
        if (normalized.Contains("process")) return AcompanhamentoServicoTipo.PROCESSOS_ADM;
        return fallback;
    }

    private static bool TryDecimal(string? text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        var clean = text.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, PtBr, out var parsed)
            || decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    private static bool TryDate(string? text, out DateOnly? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var serial) && serial is > 0 and < 2958466)
        {
            value = DateOnly.FromDateTime(DateTime.FromOADate(serial));
            return true;
        }
        if (DateOnly.TryParse(text, PtBr, DateTimeStyles.None, out var date)
            || DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            value = date;
            return true;
        }
        return false;
    }

    private static long CreateOriginId(string code)
    {
        var digits = DigitsRegex().Replace(code, "");
        if (digits.Length > 0 && long.TryParse(digits, out var numeric) && numeric > 0) return numeric;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant()));
        return BitConverter.ToInt64(hash, 0) & long.MaxValue;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"') { current.Append('"'); index++; }
                else quoted = !quoted;
            }
            else if (character == ';' && !quoted) { result.Add(current.ToString()); current.Clear(); }
            else current.Append(character);
        }
        result.Add(current.ToString());
        return result;
    }

    private static string CanonicalHeader(string header) => header switch
    {
        "cod" or "codigo" or "origemid" or "coluna1" or "num" or "numero" or "n" or "no" or "protocolo" or "numprotocolo" or "numeroprotocolo" or "numprocesso" or "numeroprocesso" => "codigo",
        "nomedocliente" or "cliente" or "nome" or "razaosocial" or "empresa" => "nomecliente",
        "cpf" or "cnpj" or "cpfcnpj" or "cnpjcpf" or "documento" or "doc" or "numdocumento" or "numerodocumento" => "cnpjcpf",
        "descricaodasituacao" or "descricao" => "descricaosituacao",
        "rscontrato" or "rcontrato" or "valorcontrato" or "valor" or "contrato" => "contrato",
        "condicaopag" or "condicaopagamento" => "condicaopagamento",
        "endereco" or "endereço" or "local" or "logradouro" => "endereco",
        "telefone" or "celular" or "fone" or "contato" => "telefone",
        _ => header
    };

    private static string NormalizeHeader(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
                builder.Append(character);
        return builder.ToString();
    }

    [GeneratedRegex("[^0-9]")]
    private static partial Regex DigitsRegex();
}
