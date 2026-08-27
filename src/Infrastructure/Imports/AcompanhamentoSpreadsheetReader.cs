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
    private static readonly string[] RequiredHeaders = ["codigo"];
    private static readonly string[] IdentityHeaders =
        ["codigo", "nomecliente", "cnpjcpf", "endereco", "telefone", "servico", "situacao", "descricaosituacao", "nf", "datacontrato"];
    private static readonly string[] AmountHeaders = ["contrato", "areceber", "recebido", "custos"];

    public async Task<IReadOnlyList<AcompanhamentoImportPreviewDto>> ReadAsync(
        Stream stream, string fileName, AcompanhamentoServicoTipo? tipo = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("O nome do arquivo é obrigatório.", nameof(fileName));

        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".xlsb", StringComparison.OrdinalIgnoreCase))
        {
            await using var bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream, ct);
            bufferedStream.Position = 0;
            using var workbook = SpreadsheetWorkbookLoader.Load(bufferedStream, fileName);
            return ReadWorkbook(workbook, tipo);
        }
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return await ReadCsvAsync(stream, fileName, tipo, ct);
        throw new ArgumentException("Formato não suportado. Selecione um arquivo .xlsx, .xlsb ou .csv.");
    }

    // Recebe o workbook ja carregado para que a importacao integrada nao
    // precise converter o arquivo duas vezes (o .xlsb e reconstruido em
    // memoria pelo SpreadsheetWorkbookLoader, o que e caro).
    internal static IReadOnlyList<AcompanhamentoImportPreviewDto> ReadWorkbook(
        XLWorkbook workbook, AcompanhamentoServicoTipo? requestedType)
    {
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
                if (row.IsEmpty()) continue;
                string? Value(string header) => ReadCell(row, headers, header);
                if (!HasContent(Value)) continue;
                result.Add(ParseRow(rowNumber, worksheet.Name, sheetType.Value, Value));
            }
        }
        return result.Count > 0 ? result : throw new ArgumentException("Nenhuma aba compatível com acompanhamento foi encontrada no arquivo.");
    }

    // Linhas de rodape das abas costumam trazer apenas zeros de formula
    // (A Receber = 0, Recebido = 0, Custos = 0) sem codigo nem cliente.
    // Sem esse filtro elas viravam acompanhamentos "IMP-..." fantasmas.
    private static bool HasContent(Func<string, string?> value)
    {
        foreach (var header in IdentityHeaders)
            if (!string.IsNullOrWhiteSpace(value(header)))
                return true;
        foreach (var header in AmountHeaders)
            if (TryDecimal(value(header), out var amount) && amount is not null && amount != 0m)
                return true;
        return false;
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
        var code = value("codigo")?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            code = CreateImportCode(sheet, line);
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
        _ = TryDecimal(value("areceber"), out var receivable);
        _ = TryDecimal(value("recebido"), out var received);
        _ = TryDecimal(value("custos"), out var costs);
        _ = TryDecimal(value("contrato"), out var contract);
        _ = TryDate(value("datacontrato"), out var contractDate);

        // "Prox. Parcela" nem sempre é data: na aba de Processos Adm a coluna
        // é preenchida com a próxima ação ("Finalizar", "Protocolar"). Quando
        // não dá para ler como data, o texto original é preservado em vez de
        // ser jogado fora.
        var nextInstallmentRaw = value("proximaparcela")?.Trim();
        _ = TryDate(nextInstallmentRaw, out var nextInstallment);
        var nextInstallmentText = nextInstallment is null && !string.IsNullOrWhiteSpace(nextInstallmentRaw)
            ? nextInstallmentRaw
            : null;

        var dto = new AcompanhamentoImportDto(CreateOriginId(code), code, type, client, address, phone, service,
            status, description, contract, contractDate, invoice, paymentTerms, cnpjCpf, receivable, received, costs,
            nextInstallment, nextInstallmentText);
        return new(line, sheet, code, type.ToString(), client, true, null, dto);
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

        var isDateHeader = header is "datacontrato" or "proximaparcela";
        if (isDateHeader)
        {
            if (cell.TryGetValue<DateTime>(out var date))
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (cell.TryGetValue<double>(out var serial) && serial is > 0 and < 2958466)
            {
                try
                {
                    return DateTime.FromOADate(serial).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var formatted = cell.GetFormattedString();
            if (!string.IsNullOrWhiteSpace(formatted))
                return formatted;
            return cell.GetString();
        }

        // TryDecimal expects pt-BR text first. Returning an Excel number with an
        // invariant decimal point made it look like a thousands separator
        // (8851.800000000001 became 8851800000000001).
        if (cell.DataType == XLDataType.Number)
        {
            if (cell.TryGetValue<decimal>(out var number))
                return number.ToString(PtBr);
            return cell.GetDouble().ToString("G17", PtBr);
        }
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

        var clean = text.Trim();

        // Check for Excel serial number (accepting both comma and dot)
        var cleanForDouble = clean.Replace(",", ".");
        if (double.TryParse(cleanForDouble, NumberStyles.Number, CultureInfo.InvariantCulture, out var serial) && serial is > 0 and < 2958466)
        {
            try
            {
                value = DateOnly.FromDateTime(DateTime.FromOADate(serial));
                return true;
            }
            catch { }
        }

        if (DateOnly.TryParse(clean, PtBr, DateTimeStyles.None, out var date)
            || DateOnly.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            value = date;
            return true;
        }

        if (DateTime.TryParse(clean, PtBr, DateTimeStyles.None, out var dt)
            || DateTime.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            value = DateOnly.FromDateTime(dt);
            return true;
        }

        string[] formats =
        [
            "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy", "yyyy-MM-dd", "yyyy/MM/dd",
            "d/M/yyyy", "d-M-yyyy", "d.M.yyyy",
            "dd/MM/yy", "dd-MM-yy", "dd.MM.yy", "d/M/yy",
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss"
        ];
        if (DateTime.TryParseExact(clean, formats, PtBr, DateTimeStyles.None, out dt)
            || DateTime.TryParseExact(clean, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            value = DateOnly.FromDateTime(dt);
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

    private static string CreateImportCode(string sheet, int line)
    {
        var normalizedSheet = NormalizeHeader(sheet);
        return $"IMP-{(normalizedSheet.Length > 12 ? normalizedSheet[..12] : normalizedSheet)}-{line:000000}";
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
        "rscontrato" or "rcontrato" or "valorcontrato" or "valor" or "contrato" or "vlcontrato" or "vlrcontrato" => "contrato",
        "condicaopag" or "condicaopagamento" or "condicao" or "formapagamento" or "formapag" => "condicaopagamento",
        "proximaparcela" or "proxparcela" or "proximavencimento" or "proximovencimento" or "proxvencimento"
            or "vencimentoproximaparcela" or "vencimentoproxparcela" or "vencimentodaproximaparcela" or "vencimentodaproxparcela"
            or "vencimentoparcela" or "vencimentodaparcela" or "dataproxparcela" or "dtproxparcela" or "dataproximaparcela"
            or "dtdaproximaparcela" or "datadaproximaparcela" or "datadaproxparcela" or "dataprox" or "dtprox"
            or "datavencimento" or "dtvencimento" or "vencimento" or "dataparcela" or "dtparcela" or "proxparcelas"
            or "proximasparcelas" or "proximovenc" or "proxvenc" or "prox" or "proxima" or "proximo"
            or "proximadata" or "dtvenc" or "datavenc" or "1aparcela" or "primeiraparcela" or "dt1aparcela"
            or "data1aparcela" or "venc1aparcela" or "proxparcelavencimento" or "vencimentoprox" or "dtproxvenc"
            or "dataproxvenc" or "dataproxvencimento" or "dtproxvencimento" or "parcela1" or "venc" or "vencimentos"
            or "proximovencimentoparcela" or "proxvencimentoparcela" => "proximaparcela",
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
