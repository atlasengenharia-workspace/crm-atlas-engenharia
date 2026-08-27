using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Operacao;

namespace CrmAtlas.Infrastructure.Documents;

public sealed class AcompanhamentoPdfReportService : IAcompanhamentoReportService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public byte[] GeneratePdf(AcompanhamentoDto item)
    {
        var doc = new PdfDocumentBuilder("RELATÓRIO DE ACOMPANHAMENTO INDIVIDUAL");

        // Header Metadata Box
        doc.AddSectionHeader("1. DADOS DO CLIENTE E CONTRATO");
        doc.AddGridBox(new[]
        {
            ("Código do Serviço", item.Codigo),
            ("Tipo de Serviço", item.Tipo.ToString()),
            ("Cliente", item.Cliente ?? "Não informado"),
            ("CPF / CNPJ", item.CnpjCpf ?? "-"),
            ("Telefone", item.Telefone ?? "-"),
            ("Endereço", item.Endereco ?? "-"),
            ("Situação Atual", item.Situacao),
            ("Nota Fiscal (NF)", item.NotaFiscal ?? "-"),
            ("Condição Pagamento", item.CondicaoPagamento ?? "-"),
            ("Valor do Contrato", item.ValorContrato?.ToString("C2", PtBr) ?? "-"),
            ("Data do Contrato", item.DataContrato?.ToString("dd/MM/yyyy") ?? "-"),
            ("Próxima Parcela", item.ProximaParcela?.ToString("dd/MM/yyyy") ?? "-")
        });

        // Pendencies Table
        doc.AddSectionHeader("2. PENDÊNCIAS E ETAPAS OPERACIONAIS");
        if (item.Itens.Count == 0)
        {
            doc.AddTextRow("Nenhuma pendência operacional registrada.");
        }
        else
        {
            var headers = new[] { "SITUAÇÃO", "ETAPA / PENDÊNCIA", "STATUS" };
            var rows = item.Itens.Select(x => new[]
            {
                x.Concluida ? "[X]" : "[ ]",
                x.Label,
                x.Concluida ? "Concluída" : "Pendente"
            }).ToList();
            doc.AddTable(headers, rows, new[] { 70f, 380f, 95f });
        }

        // History Section
        doc.AddSectionHeader("3. HISTÓRICO DE MUDANÇAS");
        if (item.Historicos.Count == 0)
        {
            doc.AddTextRow("Nenhum histórico registrado.");
        }
        else
        {
            var headers = new[] { "DATA / HORA", "NOVA SITUAÇÃO", "RESPONSÁVEL" };
            var rows = item.Historicos.Select(x => new[]
            {
                x.Em.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                x.Nova,
                x.Responsavel ?? "Sistema"
            }).ToList();
            doc.AddTable(headers, rows, new[] { 130f, 260f, 155f });
        }

        return doc.Build();
    }

    public byte[] GenerateExcel(IEnumerable<AcompanhamentoDto> items)
    {
        var list = items.ToList();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Acompanhamentos");
        var headers = new[] { "Código", "Tipo", "Cliente", "CPF/CNPJ", "Endereço", "Telefone", "Serviço", "R$ Contrato", "Data Contrato", "NF", "Condição Pagamento", "Próxima Parcela", "Situação", "Observações", "A Receber", "Recebido", "Custos", "Atualização" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(15, 27, 45));
            cell.Style.Font.FontColor = XLColor.White;
        }
        for (int r = 0; r < list.Count; r++)
        {
            var x = list[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = x.Codigo;
            ws.Cell(row, 2).Value = x.Tipo.ToString();
            ws.Cell(row, 3).Value = x.Cliente;
            ws.Cell(row, 4).Value = x.CnpjCpf;
            ws.Cell(row, 5).Value = x.Endereco;
            ws.Cell(row, 6).Value = x.Telefone;
            ws.Cell(row, 7).Value = x.Servico;
            ws.Cell(row, 8).Value = x.ValorContrato;
            ws.Cell(row, 8).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(row, 9).Value = x.DataContrato?.ToString("dd/MM/yyyy");
            ws.Cell(row, 10).Value = x.NotaFiscal;
            ws.Cell(row, 11).Value = x.CondicaoPagamento;
            ws.Cell(row, 12).Value = x.ProximaParcela?.ToString("dd/MM/yyyy") ?? x.ProximaParcelaTexto;
            ws.Cell(row, 13).Value = x.Situacao;
            ws.Cell(row, 14).Value = x.Descricao;
            ws.Cell(row, 15).Value = x.AReceber;
            ws.Cell(row, 15).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(row, 16).Value = x.Recebido;
            ws.Cell(row, 16).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(row, 17).Value = x.Custos;
            ws.Cell(row, 17).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(row, 18).Value = x.AtualizadoEm?.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenerateGeneralOperationalReport(IEnumerable<AcompanhamentoDto> items)
    {
        var list = items.ToList();
        var doc = new PdfDocumentBuilder("RELATÓRIO GERAL DE ACOMPANHAMENTOS OPERACIONAIS");

        // KPI Summary Box
        doc.AddSectionHeader("1. INDICADORES CHAVE DE DESEMPENHO (KPIs)");
        doc.AddKpiCards(new[]
        {
            ("Total Serviços", list.Count.ToString("N0", PtBr)),
            ("Projetos AVCB", list.Count(x => x.Tipo == AcompanhamentoServicoTipo.AVCB).ToString("N0", PtBr)),
            ("Projetos CLCB", list.Count(x => x.Tipo == AcompanhamentoServicoTipo.CLCB).ToString("N0", PtBr)),
            ("Obras / Processos", (list.Count(x => x.Tipo == AcompanhamentoServicoTipo.OBRAS) + list.Count(x => x.Tipo == AcompanhamentoServicoTipo.PROCESSOS_ADM)).ToString("N0", PtBr)),
            ("Total Contratado", list.Sum(x => x.ValorContrato ?? 0).ToString("C2", PtBr))
        });

        // Main Data Table
        doc.AddSectionHeader("2. LISTAGEM DE SERVIÇOS EM ACOMPANHAMENTO");
        var headers = new[] { "CÓDIGO", "TIPO", "CLIENTE / EMPRESA", "SITUAÇÃO", "CONTRATO" };
        var colWidths = new[] { 70f, 75f, 200f, 110f, 90f };

        var rows = list.Take(25).Select(x => new[]
        {
            x.Codigo,
            x.Tipo.ToString(),
            Truncate(x.Cliente ?? "Cliente s/ nome", 28),
            Truncate(x.Situacao, 16),
            x.ValorContrato?.ToString("C2", PtBr) ?? "R$ 0,00"
        }).ToList();

        doc.AddTable(headers, rows, colWidths);

        if (list.Count > 25)
        {
            doc.AddTextRow($"... e mais {list.Count - 25} serviço(s) não exibidos nesta visualização impressa.");
        }

        return doc.Build();
    }

    public byte[] GeneratePurchaseOrderReport(string prestador, string escopo, decimal valor, string condicao)
    {
        var doc = new PdfDocumentBuilder("PEDIDO DE COMPRA E SERVIÇO (ORDEM DE SERVIÇO)");

        doc.AddSectionHeader("1. DADOS DO FORNECEDOR E CONDIÇÕES");
        doc.AddGridBox(new[]
        {
            ("Número do Pedido", $"PO-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}"),
            ("Data da Emissão", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
            ("Prestador / Fornecedor", string.IsNullOrWhiteSpace(prestador) ? "Não especificado" : prestador),
            ("Valor Total Acordado", valor.ToString("C2", PtBr)),
            ("Condição de Pagamento", string.IsNullOrWhiteSpace(condicao) ? "A combinar" : condicao),
            ("Status do Pedido", "Aguardando Aprovação")
        });

        doc.AddSectionHeader("2. ESCOPO DETALHADO DO SERVIÇO / FORNECIMENTO");
        doc.AddTextBox(string.IsNullOrWhiteSpace(escopo) ? "Nenhum detalhe adicional informado." : escopo);

        doc.AddSectionHeader("3. APROVAÇÕES E ASSINATURAS REQUERIDAS");
        doc.AddSignatureBoxes("Gerência Operacional", "Diretoria Financeira");

        return doc.Build();
    }

    public byte[] GenerateFinancialSummaryReport(decimal faturamento, decimal custos, decimal lucro, int totalLancamentos)
    {
        var doc = new PdfDocumentBuilder("DEMONSTRATIVO FINANCEIRO EXECUTIVO");

        var margem = faturamento > 0 ? (lucro / faturamento * 100) : 0m;

        doc.AddSectionHeader("1. BALANÇO FINANCEIRO CONSOLIDADO");
        doc.AddKpiCards(new[]
        {
            ("Faturamento Bruto", faturamento.ToString("C2", PtBr)),
            ("Custos Totais", custos.ToString("C2", PtBr)),
            ("Resultado (Lucro)", lucro.ToString("C2", PtBr)),
            ("Margem Operacional", $"{margem:N1}%"),
            ("Total Lançamentos", totalLancamentos.ToString("N0", PtBr))
        });

        doc.AddSectionHeader("2. DETALHAMENTO DAS CONTAS E RESULTADOS");
        var headers = new[] { "CATEGORIA FINANCEIRA", "VALOR CONSOLIDADO", "PARTICIPAÇÃO %" };
        var colWidths = new[] { 260f, 150f, 135f };

        var rows = new List<string[]>
        {
            new[] { "Faturamento / Entradas Confirmadas", faturamento.ToString("C2", PtBr), "100.0%" },
            new[] { "Custos Diretos e Indiretos Operacionais", custos.ToString("C2", PtBr), faturamento > 0 ? $"{(custos / faturamento * 100):N1}%" : "0.0%" },
            new[] { "LUCRO LÍQUIDO OPERACIONAL", lucro.ToString("C2", PtBr), $"{margem:N1}%" }
        };

        doc.AddTable(headers, rows, colWidths);

        doc.AddSectionHeader("3. NOTAS EXPLICATIVAS E CONFIRMAÇÃO");
        doc.AddTextBox("Este demonstrativo financeiro foi compilado a partir dos lançamentos validados no CRM Atlas. " +
                       "Os valores de custos englobam despesas diretas com serviços e custos indiretos do período.");

        return doc.Build();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";

    #region Professional PDF Builder

    private sealed class PdfDocumentBuilder
    {
        private readonly string _title;
        private readonly List<PdfElement> _elements = new();

        public PdfDocumentBuilder(string title)
        {
            _title = title;
        }

        public void AddSectionHeader(string text) => _elements.Add(new SectionHeaderElement(text));
        public void AddGridBox(IEnumerable<(string Key, string Value)> pairs) => _elements.Add(new GridBoxElement(pairs.ToList()));
        public void AddTable(string[] headers, List<string[]> rows, float[] widths) => _elements.Add(new TableElement(headers, rows, widths));
        public void AddKpiCards(IEnumerable<(string Title, string Value)> cards) => _elements.Add(new KpiCardsElement(cards.ToList()));
        public void AddTextBox(string text) => _elements.Add(new TextBoxElement(text));
        public void AddTextRow(string text) => _elements.Add(new TextRowElement(text));
        public void AddSignatureBoxes(string leftTitle, string rightTitle) => _elements.Add(new SignatureBoxesElement(leftTitle, rightTitle));

        public byte[] Build()
        {
            var streamContent = new StringBuilder();

            // Setup Graphic State and Fonts
            streamContent.AppendLine("q");

            // Top Corporate Header Banner (Dark Blue #0F1B2D with Accent Blue #2563EB)
            streamContent.AppendLine("0.058 0.105 0.176 rg 0 770 595 72 re f"); // Header Background
            streamContent.AppendLine("0.145 0.388 0.921 rg 0 766 595 4 re f"); // Accent Blue Strip

            // Header Logo Text & Title
            streamContent.AppendLine("BT /F2 14 Tf 1.0 1.0 1.0 rg 40 812 Td (ATLAS ENGENHARIA E GESTAO) Tj ET");
            streamContent.AppendLine($"BT /F1 9 Tf 0.8 0.9 1.0 rg 40 782 Td ({EscapePdf(_title)}) Tj ET");
            streamContent.AppendLine($"BT /F1 8 Tf 0.8 0.9 1.0 rg 430 782 Td (Emissao: {DateTime.Now:dd/MM/yyyy HH:mm}) Tj ET");

            float y = 740f;

            foreach (var elem in _elements)
            {
                y = elem.Render(streamContent, y);
            }

            // Footer Section
            streamContent.AppendLine("0.145 0.388 0.921 rg 40 45 515 1.5 re f");
            streamContent.AppendLine("BT /F1 8 Tf 0.39 0.45 0.54 rg 40 30 Td (CRM Atlas v2.4.0 - Documento Oficial de Gestao Operacional e Financeira) Tj ET");
            streamContent.AppendLine("BT /F1 8 Tf 0.39 0.45 0.54 rg 490 30 Td (Pagina 1 de 1) Tj ET");

            streamContent.AppendLine("Q");

            var pdfStream = streamContent.ToString();
            var streamLength = Encoding.Latin1.GetByteCount(pdfStream);

            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {streamLength} >>\nstream\n{pdfStream}endstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
            };

            using var output = new MemoryStream();
            WriteRaw(output, "%PDF-1.4\n%âãÏÓ\n");
            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(output.Position);
                WriteRaw(output, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }
            var xref = output.Position;
            WriteRaw(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1)) WriteRaw(output, $"{offset:0000000000} 00000 n \n");
            WriteRaw(output, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
            return output.ToArray();
        }
    }

    private abstract class PdfElement
    {
        public abstract float Render(StringBuilder sb, float startY);
    }

    private sealed class SectionHeaderElement(string title) : PdfElement
    {
        public override float Render(StringBuilder sb, float startY)
        {
            float y = startY - 20f;
            // Accent Vertical Bar + Section Title Background
            sb.AppendLine($"0.145 0.388 0.921 rg 40 {y - 2} 4 16 re f");
            sb.AppendLine($"BT /F2 10 Tf 0.058 0.105 0.176 rg 52 {y} Td ({EscapePdf(title)}) Tj ET");
            return y - 10f;
        }
    }

    private sealed class GridBoxElement(List<(string Key, string Value)> pairs) : PdfElement
    {
        public override float Render(StringBuilder sb, float startY)
        {
            float x1 = 40f, x2 = 300f;
            float rowHeight = 18f;
            float currentY = startY - 5f;

            // Draw Background Box
            float totalHeight = ((pairs.Count + 1) / 2) * rowHeight + 10f;
            sb.AppendLine($"0.97 0.98 0.99 rg 40 {currentY - totalHeight} 515 {totalHeight} re f");
            sb.AppendLine($"0.85 0.88 0.92 RG 1 w 40 {currentY - totalHeight} 515 {totalHeight} re S");

            currentY -= 14f;
            for (int i = 0; i < pairs.Count; i++)
            {
                float x = (i % 2 == 0) ? x1 + 10f : x2 + 10f;
                var pair = pairs[i];

                sb.AppendLine($"BT /F2 8 Tf 0.39 0.45 0.54 rg {x} {currentY} Td ({EscapePdf(pair.Key)}:) Tj ET");
                sb.AppendLine($"BT /F1 8.5 Tf 0.058 0.105 0.176 rg {x + 95f} {currentY} Td ({EscapePdf(pair.Value)}) Tj ET");

                if (i % 2 == 1 || i == pairs.Count - 1)
                {
                    currentY -= rowHeight;
                }
            }

            return startY - totalHeight - 15f;
        }
    }

    private sealed class KpiCardsElement(List<(string Title, string Value)> cards) : PdfElement
    {
        public override float Render(StringBuilder sb, float startY)
        {
            float y = startY - 45f;
            float cardWidth = 515f / cards.Count;

            for (int i = 0; i < cards.Count; i++)
            {
                float x = 40f + (i * cardWidth);
                var card = cards[i];

                // Card Background Box
                sb.AppendLine($"0.96 0.97 0.99 rg {x + 2} {y} {cardWidth - 4} 40 re f");
                sb.AppendLine($"0.85 0.88 0.92 RG 1 w {x + 2} {y} {cardWidth - 4} 40 re S");
                sb.AppendLine($"0.145 0.388 0.921 rg {x + 2} {y + 37} {cardWidth - 4} 3 re f");

                sb.AppendLine($"BT /F1 7.5 Tf 0.39 0.45 0.54 rg {x + 8} {y + 24} Td ({EscapePdf(card.Title)}) Tj ET");
                sb.AppendLine($"BT /F2 10 Tf 0.058 0.105 0.176 rg {x + 8} {y + 9} Td ({EscapePdf(card.Value)}) Tj ET");
            }

            return y - 15f;
        }
    }

    private sealed class TableElement(string[] headers, List<string[]> rows, float[] widths) : PdfElement
    {
        public override float Render(StringBuilder sb, float startY)
        {
            float y = startY - 18f;

            // Draw Header Row (Dark Blue Background #0F1B2D)
            sb.AppendLine($"0.058 0.105 0.176 rg 40 {y} 515 18 re f");

            float curX = 40f;
            for (int i = 0; i < headers.Length; i++)
            {
                sb.AppendLine($"BT /F2 8 Tf 1.0 1.0 1.0 rg {curX + 6} {y + 5} Td ({EscapePdf(headers[i])}) Tj ET");
                curX += widths[i];
            }

            y -= 16f;

            // Render Rows
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];

                // Zebra Striping
                if (r % 2 == 1)
                {
                    sb.AppendLine($"0.96 0.97 0.98 rg 40 {y} 515 16 re f");
                }

                sb.AppendLine($"0.88 0.90 0.93 RG 0.5 w 40 {y} 515 16 re S");

                curX = 40f;
                for (int c = 0; c < Math.Min(row.Length, headers.Length); c++)
                {
                    sb.AppendLine($"BT /F1 8 Tf 0.058 0.105 0.176 rg {curX + 6} {y + 4} Td ({EscapePdf(row[c])}) Tj ET");
                    curX += widths[c];
                }

                y -= 16f;
            }

            return y - 10f;
        }
    }

    private sealed class TextBoxElement(string text) : PdfElement
    {
        public override float Render(StringBuilder sb, float startY)
        {
            float y = startY - 60f;
            sb.AppendLine($"0.97 0.98 0.99 rg 40 {y} 515 55 re f");
            sb.AppendLine($"0.85 0.88 0.92 RG 1 w 40 {y} 515 55 re S");

            var wrappedLines = WrapText(text, 90).Take(3).ToList();
            float textY = y + 38f;
            foreach (var line in wrappedLines)
            {
                sb.AppendLine($"BT /F1 8.5 Tf 0.2 0.25 0.35 rg 50 {textY} Td ({EscapePdf(line)}) Tj ET");
                textY -= 14f;
            }

            return y - 15f;
        }
    }

    private sealed class TextRowElement(string text) : PdfElement
    {
        public override float Render(StringBuilder sb, float startY)
        {
            float y = startY - 15f;
            sb.AppendLine($"BT /F1 8.5 Tf 0.39 0.45 0.54 rg 45 {y} Td ({EscapePdf(text)}) Tj ET");
            return y - 5f;
        }
    }

    private sealed class SignatureBoxesElement(string leftTitle, string rightTitle) : PdfElement
    {
        public override float Render(StringBuilder sb, float startY)
        {
            float y = startY - 60f;

            // Left Signature Box
            sb.AppendLine($"0.85 0.88 0.92 RG 1 w 50 {y} 220 50 re S");
            sb.AppendLine($"0.145 0.388 0.921 rg 70 {y + 20} 180 1 re f");
            sb.AppendLine($"BT /F2 8 Tf 0.39 0.45 0.54 rg 85 {y + 8} Td ({EscapePdf(leftTitle)}) Tj ET");

            // Right Signature Box
            sb.AppendLine($"0.85 0.88 0.92 RG 1 w 325 {y} 220 50 re S");
            sb.AppendLine($"0.145 0.388 0.921 rg 345 {y + 20} 180 1 re f");
            sb.AppendLine($"BT /F2 8 Tf 0.39 0.45 0.54 rg 365 {y + 8} Td ({EscapePdf(rightTitle)}) Tj ET");

            return y - 15f;
        }
    }

    private static IEnumerable<string> WrapText(string text, int width)
    {
        if (text.Length <= width) { yield return text; yield break; }
        for (var offset = 0; offset < text.Length; offset += width)
            yield return text.Substring(offset, Math.Min(width, text.Length - offset));
    }

    private static string EscapePdf(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
             .Replace("ã", "a").Replace("ç", "c").Replace("é", "e").Replace("á", "a")
             .Replace("ó", "o").Replace("í", "i").Replace("ú", "u").Replace("ê", "e")
             .Replace("õ", "o").Replace("à", "a").Replace("Â", "A").Replace("Ç", "C");

    private static void WriteRaw(Stream stream, string value) =>
        stream.Write(Encoding.Latin1.GetBytes(value));

    #endregion
}
