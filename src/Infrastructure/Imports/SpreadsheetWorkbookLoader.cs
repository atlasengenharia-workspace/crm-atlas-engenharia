using ClosedXML.Excel;
using ExcelDataReader;
using System.Text;

namespace CrmAtlas.Infrastructure.Imports;

internal static class SpreadsheetWorkbookLoader
{
    // Abas auxiliares de planilha (DADOS, GRAFICOS, apoio de tabela dinamica)
    // normalmente tem formulas arrastadas ate a ultima linha do Excel
    // (1.048.576). Materializar isso no ClosedXML custa varios GB de RAM e
    // trava a importacao. Os limites abaixo cortam essa cauda sem depender do
    // nome da aba: paramos a aba depois de uma sequencia longa de linhas
    // "vazias" (celulas em branco ou apenas zeros) e nunca passamos do teto.
    private const int MaxRowsPerSheet = 60_000;
    private const int MaxTrailingBlankRows = 500;

    static SpreadsheetWorkbookLoader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static XLWorkbook Load(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            if (stream.CanSeek) stream.Position = 0;
            return new XLWorkbook(stream);
        }

        if (!extension.Equals(".xlsb", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Formato não suportado. Selecione um arquivo .xlsx ou .xlsb.");

        if (stream.CanSeek) stream.Position = 0;
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var workbook = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sheetIndex = 0;

        do
        {
            var worksheet = workbook.AddWorksheet(UniqueSheetName(reader.Name, ++sheetIndex, usedNames));
            var rowNumber = 0;
            var blankStreak = 0;
            while (rowNumber < MaxRowsPerSheet && blankStreak < MaxTrailingBlankRows && reader.Read())
            {
                rowNumber++;
                var hasRealValue = false;
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    var value = reader.GetValue(column);
                    if (value is null) continue;
                    SetCellValue(worksheet.Cell(rowNumber, column + 1), value);
                    if (!IsFillerValue(value)) hasRealValue = true;
                }
                blankStreak = hasRealValue ? 0 : blankStreak + 1;
            }
        } while (reader.NextResult());

        return workbook;
    }

    // Zero e string vazia sao o resultado tipico de formula arrastada em linha
    // sem dado. O valor continua sendo gravado na celula (nada se perde); ele
    // so nao conta como "linha com conteudo" na deteccao do fim da aba.
    private static bool IsFillerValue(object value) => value switch
    {
        string text => text.Trim().Length == 0,
        byte number => number == 0,
        short number => number == 0,
        int number => number == 0,
        long number => number == 0,
        float number => number == 0,
        double number => number == 0,
        decimal number => number == 0,
        _ => false
    };

    private static void SetCellValue(IXLCell cell, object value)
    {
        switch (value)
        {
            case DateTime dateTime:
                cell.SetValue(dateTime);
                break;
            case DateTimeOffset dateTimeOffset:
                cell.SetValue(dateTimeOffset.DateTime);
                break;
            case bool boolean:
                cell.SetValue(boolean);
                break;
            case byte number:
                cell.SetValue(number);
                break;
            case short number:
                cell.SetValue(number);
                break;
            case int number:
                cell.SetValue(number);
                break;
            case long number:
                cell.SetValue(number);
                break;
            case float number:
                cell.SetValue(number);
                break;
            case double number:
                cell.SetValue(number);
                break;
            case decimal number:
                cell.SetValue(number);
                break;
            default:
                cell.SetValue(value.ToString() ?? string.Empty);
                break;
        }
    }

    private static string UniqueSheetName(string? sourceName, int index, ISet<string> usedNames)
    {
        var name = string.IsNullOrWhiteSpace(sourceName) ? $"Planilha {index}" : sourceName.Trim();
        foreach (var invalid in new[] { ':', '\\', '/', '?', '*', '[', ']' })
            name = name.Replace(invalid, '_');
        name = name.Trim().Trim('\'').Trim();
        if (name.Length == 0) name = $"Planilha {index}";
        if (name.Length > 31) name = name[..31];

        var candidate = name;
        var suffix = 2;
        while (!usedNames.Add(candidate))
        {
            var suffixText = $" ({suffix++})";
            candidate = name[..Math.Min(name.Length, 31 - suffixText.Length)] + suffixText;
        }
        return candidate;
    }
}
