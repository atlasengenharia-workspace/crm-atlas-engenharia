using System.Globalization;
using System.Text;
using CrmAtlas.ApplicationCore.Operacao;

namespace CrmAtlas.Infrastructure.Documents;

public sealed class AcompanhamentoPdfReportService : IAcompanhamentoReportService
{
    public byte[] GeneratePdf(AcompanhamentoDto item)
    {
        var lines = new List<string>
        {
            "ATLAS ENGENHARIA",
            "Relatório de acompanhamento de serviço",
            "",
            $"Código: {item.Codigo}",
            $"Tipo: {item.Tipo}",
            $"Cliente: {item.Cliente ?? "-"}",
            $"Situação atual: {item.Situacao}",
            $"Contrato: {(item.ValorContrato?.ToString("C2", new CultureInfo("pt-BR")) ?? "-")}",
            $"Data do contrato: {(item.DataContrato?.ToString("dd/MM/yyyy") ?? "-")}",
            "",
            "Pendências"
        };
        lines.AddRange(item.Itens.Select(x => $"{(x.Concluida ? "[X]" : "[ ]")} {x.Label}"));
        lines.Add(""); lines.Add("Histórico");
        lines.AddRange(item.Historicos.Select(x => $"{x.Em.ToLocalTime():dd/MM/yyyy HH:mm} - {x.Nova} - {x.Responsavel ?? "Sistema"}"));
        lines.Add(""); lines.Add($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}");
        return BuildPdf(lines);
    }

    private static byte[] BuildPdf(IEnumerable<string> source)
    {
        var lines = source.SelectMany(Wrap).Take(48).ToList();
        var content = new StringBuilder("BT\n/F1 12 Tf\n50 790 Td\n15 TL\n");
        foreach (var line in lines) content.Append('(').Append(Escape(line)).Append(") Tj\nT*\n");
        content.Append("ET");
        var stream = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.Latin1.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };
        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(output.Position);
            Write(output, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = output.Position;
        Write(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static IEnumerable<string> Wrap(string text)
    {
        const int width = 82;
        if (text.Length <= width) { yield return text; yield break; }
        for (var offset = 0; offset < text.Length; offset += width)
            yield return text.Substring(offset, Math.Min(width, text.Length - offset));
    }
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static void Write(Stream stream, string value) => stream.Write(Encoding.Latin1.GetBytes(value));
}
