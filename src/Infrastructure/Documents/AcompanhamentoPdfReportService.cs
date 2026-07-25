using System.Globalization;
using System.Text;
using CrmAtlas.ApplicationCore.Operacao;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.Infrastructure.Documents;

public sealed class AcompanhamentoPdfReportService : IAcompanhamentoReportService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public byte[] GeneratePdf(AcompanhamentoDto item)
    {
        var lines = new List<string>
        {
            "ATLAS ENGENHARIA — RELATÓRIO DE SERVIÇO INDIVIDUAL",
            "----------------------------------------------------------------------------------",
            $"Código do Serviço: {item.Codigo}",
            $"Tipo de Serviço: {item.Tipo}",
            $"Cliente: {item.Cliente ?? "-"}",
            $"CPF / CNPJ: {item.CnpjCpf ?? "-"}",
            $"Telefone: {item.Telefone ?? "-"}",
            $"Endereço: {item.Endereco ?? "-"}",
            $"Situação Atual: {item.Situacao}",
            $"Nota Fiscal (NF): {item.NotaFiscal ?? "-"}",
            $"Condição de Pagamento: {item.CondicaoPagamento ?? "-"}",
            $"Valor Contratado: {(item.ValorContrato?.ToString("C2", PtBr) ?? "-")}",
            $"Data do Contrato: {(item.DataContrato?.ToString("dd/MM/yyyy") ?? "-")}",
            "",
            "--- PENDÊNCIAS E ETAPAS ---"
        };

        if (item.Itens.Count == 0)
        {
            lines.Add("Nenhuma pendência cadastrada.");
        }
        else
        {
            lines.AddRange(item.Itens.Select(x => $"{(x.Concluida ? "[CONCLUÍDO]" : "[PENDENTE]  ")} {x.Label}"));
        }

        lines.Add("");
        lines.Add("--- HISTÓRICO DE ALTERAÇÕES ---");
        if (item.Historicos.Count == 0)
        {
            lines.Add("Nenhum histórico registrado.");
        }
        else
        {
            lines.AddRange(item.Historicos.Select(x => $"{x.Em.ToLocalTime():dd/MM/yyyy HH:mm} - {x.Nova} ({x.Responsavel ?? "Sistema"})"));
        }

        lines.Add("");
        lines.Add($"Relatório emitido em {DateTime.Now:dd/MM/yyyy HH:mm:ss} pelo CRM Atlas");
        return BuildPdf(lines);
    }

    public byte[] GenerateGeneralOperationalReport(IEnumerable<AcompanhamentoDto> items)
    {
        var list = items.ToList();
        var lines = new List<string>
        {
            "ATLAS ENGENHARIA — RELATÓRIO GERAL DE ACOMPANHAMENTO OPERACIONAL",
            "----------------------------------------------------------------------------------",
            $"Total de Serviços Registrados: {list.Count}",
            $"Total AVCB: {list.Count(x => x.Tipo == AcompanhamentoServicoTipo.AVCB)}",
            $"Total CLCB: {list.Count(x => x.Tipo == AcompanhamentoServicoTipo.CLCB)}",
            $"Total Obras: {list.Count(x => x.Tipo == AcompanhamentoServicoTipo.OBRAS)}",
            $"Total Processos Adm: {list.Count(x => x.Tipo == AcompanhamentoServicoTipo.PROCESSOS_ADM)}",
            $"Valor Total de Contratos: {list.Sum(x => x.ValorContrato ?? 0):C2}",
            "",
            "--- RESUMO DOS SERVIÇOS RECENTES ---"
        };

        foreach (var item in list.Take(30))
        {
            lines.Add($"[{item.Codigo}] {item.Tipo} - {item.Cliente ?? "Cliente s/ nome"} | Status: {item.Situacao} | Valor: {(item.ValorContrato?.ToString("C2", PtBr) ?? "R$ 0,00")}");
        }

        lines.Add("");
        lines.Add($"Documento compilado em {DateTime.Now:dd/MM/yyyy HH:mm:ss} via CRM Atlas");
        return BuildPdf(lines);
    }

    public byte[] GeneratePurchaseOrderReport(string prestador, string escopo, decimal valor, string condicao)
    {
        var lines = new List<string>
        {
            "ATLAS ENGENHARIA — PEDIDO DE COMPRA E SERVIÇO",
            "----------------------------------------------------------------------------------",
            $"Data da Solicitação: {DateTime.Now:dd/MM/yyyy}",
            $"Prestador / Fornecedor: {(string.IsNullOrWhiteSpace(prestador) ? "Não especificado" : prestador)}",
            $"Valor Total: {valor:C2}",
            $"Condição de Pagamento: {(string.IsNullOrWhiteSpace(condicao) ? "A combinar" : condicao)}",
            "",
            "--- ESCOPO DO SERVIÇO / PRODUTO ---",
            string.IsNullOrWhiteSpace(escopo) ? "Nenhum detalhe adicional informado." : escopo,
            "",
            "----------------------------------------------------------------------------------",
            "Aprovações:",
            "  [  ] Gerência Operacional",
            "  [  ] Diretoria Financeira",
            "",
            $"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm:ss} pelo CRM Atlas"
        };
        return BuildPdf(lines);
    }

    public byte[] GenerateFinancialSummaryReport(decimal faturamento, decimal custos, decimal lucro, int totalLancamentos)
    {
        var lines = new List<string>
        {
            "ATLAS ENGENHARIA — DEMONSTRATIVO FINANCEIRO EXECUTIVO",
            "----------------------------------------------------------------------------------",
            $"Período de Referência: Mês Atual ({DateTime.Now:MMMM / yyyy})",
            $"Total de Lançamentos Processados: {totalLancamentos}",
            "",
            "--- BALANÇO CONSOLIDADO ---",
            $"Faturamento Bruto Entradas: {faturamento:C2}",
            $"Custos Diretos e Indiretos: {custos:C2}",
            $"Resultado Líquido / Lucro: {lucro:C2}",
            $"Margem Operacional Estimada: {(faturamento > 0 ? (lucro / faturamento * 100).ToString("N1", PtBr) : "0")}%",
            "",
            "----------------------------------------------------------------------------------",
            "Nota: Os dados apresentados consideram todos os lançamentos financeiros validados",
            "no período ativo do CRM Atlas.",
            "",
            $"Relatório gerado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}"
        };
        return BuildPdf(lines);
    }

    private static byte[] BuildPdf(IEnumerable<string> source)
    {
        var lines = source.SelectMany(Wrap).Take(48).ToList();
        var content = new StringBuilder("BT\n/F1 11 Tf\n50 790 Td\n15 TL\n");
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
