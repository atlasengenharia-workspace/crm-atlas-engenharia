using ClosedXML.Excel;
using CrmAtlas.Infrastructure.Data;
using CrmAtlas.Infrastructure.Imports;
using Microsoft.EntityFrameworkCore;

namespace CrmAtlas.IntegrationTests;

public sealed class AtlasWorkbookImportServiceTests
{
    [Fact]
    public async Task ImportsAllModulesAndIsIdempotent()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AtlasDbContext(options);
        var service = new AtlasWorkbookImportService(db, new AcompanhamentoSpreadsheetReader());
        var content = CreateWorkbook();

        var first = await service.ImportAsync(new MemoryStream(content), "PLANILHA PURA.xlsx");
        db.ChangeTracker.Clear();
        var second = await service.ImportAsync(new MemoryStream(content), "PLANILHA PURA.xlsx");

        Assert.Equal(2, first.ClientesCriados);
        Assert.Equal(1, first.CondicoesPagamentoCriadas);
        Assert.Equal(2, first.ServicosCriados);
        Assert.Equal(2, first.AcompanhamentosCriados);
        Assert.Equal(2, first.LancamentosCriados);
        Assert.Equal(1, first.CustosIndiretosCriados);
        Assert.Equal(0, second.ClientesCriados);
        Assert.Equal(0, second.ServicosCriados);
        Assert.Equal(0, second.LancamentosCriados);
        Assert.Equal(2, await db.CadastrosServico.CountAsync());
        Assert.Equal(2, await db.Acompanhamentos.CountAsync());
    }

    private static byte[] CreateWorkbook()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            AddOperationalSheet(workbook, "AVCB", "3100", "Cliente AVCB", "AVCB");
            AddOperationalSheet(workbook, "CLCB", "4100", "Cliente CLCB", null, "Coluna1");
            var launch = workbook.AddWorksheet("LANÇAMENTOS");
            launch.Cell("A1").Value = "COD."; launch.Cell("B1").Value = "Descrição";
            launch.Cell("C1").Value = "Faturamento"; launch.Cell("D1").Value = "Data";
            launch.Cell("E1").Value = "Custo direto"; launch.Cell("G1").Value = "Observação";
            launch.Cell("A2").Value = "3100"; launch.Cell("B2").Value = "Serviço";
            launch.Cell("C2").Value = 1000; launch.Cell("D2").Value = new DateTime(2026, 7, 1);
            launch.Cell("E2").Value = 200;
            var costs = workbook.AddWorksheet("CUSTOS INDIRETOS");
            costs.Cell("A1").Value = "DATA"; costs.Cell("B1").Value = "DESCRIÇÃO";
            costs.Cell("C1").Value = "VALOR"; costs.Cell("D1").Value = "CATEGORIA";
            costs.Cell("A2").Value = new DateTime(2026, 7, 2); costs.Cell("B2").Value = "Escritório";
            costs.Cell("C2").Value = 300; costs.Cell("D2").Value = "Administrativo";
            workbook.SaveAs(stream);
        }
        return stream.ToArray();
    }

    private static void AddOperationalSheet(
        XLWorkbook workbook, string sheetName, string code, string client, string? service, string codeHeader = "Cod.")
    {
        var sheet = workbook.AddWorksheet(sheetName);
        var headers = new[] { codeHeader, "Nome do cliente", "Endereço", "Telefone", "Serviço",
            "Situação", "Descrição da situação", "R$ Contrato", "Data Contrato", "NF", "Condição pag." };
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        sheet.Cell("A2").Value = code; sheet.Cell("B2").Value = client; sheet.Cell("C2").Value = "Campinas";
        sheet.Cell("D2").Value = "19999999999"; sheet.Cell("E2").Value = service ?? "";
        sheet.Cell("F2").Value = "Concluído"; sheet.Cell("H2").Value = 1200;
        sheet.Cell("I2").Value = new DateTime(2026, 7, 1); sheet.Cell("K2").Value = "2x";
    }
}
