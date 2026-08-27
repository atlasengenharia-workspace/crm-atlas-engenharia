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
        var service = new AtlasWorkbookImportService(db);
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
        var importedTracking = await db.Acompanhamentos.SingleAsync(x => x.Codigo == "3100");
        Assert.Equal(800m, importedTracking.AReceber);
        Assert.Equal(200m, importedTracking.Recebido);
        Assert.Equal(100m, importedTracking.Custos);
        // Contrato de 1200 em "2x" (600 + 600) com 200 recebidos: a primeira
        // parcela ainda não foi coberta, então o próximo vencimento é a data
        // do contrato. A planilha não traz data nessa coluna — o valor sai do
        // parcelamento gerado.
        Assert.Equal(new DateOnly(2026, 7, 1), importedTracking.ProximaParcela);
    }

    [Fact]
    public async Task DescartaRodapeZeradoEPreservaRepeticoesFinanceiras()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AtlasDbContext(options);
        var service = new AtlasWorkbookImportService(db);

        var result = await service.ImportAsync(new MemoryStream(CreateNoisyWorkbook()), "PLANILHA PURA.xlsx");

        // Rodapé com apenas zeros de fórmula não pode virar acompanhamento.
        Assert.Equal(1, result.AcompanhamentosCriados);
        // Linha de lançamento sem descrição continua sendo dinheiro e precisa entrar.
        Assert.Equal(2, result.LancamentosCriados);
        // A mesma despesa pode se repetir no mesmo dia: as duas linhas contam.
        Assert.Equal(2, result.CustosIndiretosCriados);
        Assert.All(await db.Lancamentos.ToListAsync(), x => Assert.False(string.IsNullOrWhiteSpace(x.Descricao)));
    }

    [Fact]
    public async Task ReimportaLinhaSemNomeDeClienteSemDuplicarDocumento()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AtlasDbContext(options);
        var service = new AtlasWorkbookImportService(db);
        var content = CreateWorkbookWithoutClientName();

        var first = await service.ImportAsync(new MemoryStream(content), "PLANILHA PURA.xlsx");
        db.ChangeTracker.Clear();
        var second = await service.ImportAsync(new MemoryStream(content), "PLANILHA PURA.xlsx");

        // A linha sem nome vira RazaoSocial "Cliente do serviço X"; na segunda
        // passada a chave nome|telefone nao bate mais e, sem o indice por
        // documento, um segundo cliente com o mesmo LEG- era inserido —
        // violando IX_clientes_cnpj_cpf no PostgreSQL.
        Assert.Equal(1, first.ClientesCriados);
        Assert.Equal(0, second.ClientesCriados);
        Assert.Equal(1, await db.Clientes.CountAsync());
        Assert.Single(await db.Clientes.ToListAsync());
    }

    private static byte[] CreateWorkbookWithoutClientName()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("CLCB");
            var headers = new[] { "Coluna1", "Nome do cliente", "Endereço", "Telefone", "Situação",
                "Descrição da situação", "R$ Contrato", "NF", "Data Contrato", "A Receber", "Recebido", "Custos" };
            for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
            sheet.Cell("A2").Value = "4430";
            sheet.Cell("E2").Value = "Concluído";
            sheet.Cell("G2").Value = 1200;
            sheet.Cell("I2").Value = new DateTime(2026, 7, 1);
            workbook.SaveAs(stream);
        }
        return stream.ToArray();
    }

    private static byte[] CreateNoisyWorkbook()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            AddOperationalSheet(workbook, "AVCB", "3100", "Cliente AVCB", "AVCB");
            var operational = workbook.Worksheet("AVCB");
            // Rodapé típico da planilha real: sem código, sem cliente, só zeros.
            operational.Cell("L3").Value = 0;
            operational.Cell("M3").Value = 0;
            operational.Cell("N3").Value = 0;

            var launch = workbook.AddWorksheet("LANÇAMENTOS");
            launch.Cell("A1").Value = "COD."; launch.Cell("B1").Value = "Descrição";
            launch.Cell("C1").Value = "Faturamento"; launch.Cell("D1").Value = "Data";
            launch.Cell("E1").Value = "Custo direto"; launch.Cell("G1").Value = "Observação";
            launch.Cell("A2").Value = "3100"; launch.Cell("B2").Value = "Serviço";
            launch.Cell("C2").Value = 1000; launch.Cell("D2").Value = new DateTime(2026, 7, 1);
            // Sem descrição, mas com valor e data.
            launch.Cell("A3").Value = "3100";
            launch.Cell("C3").Value = 500; launch.Cell("D3").Value = new DateTime(2026, 7, 3);

            var costs = workbook.AddWorksheet("CUSTOS INDIRETOS");
            costs.Cell("A1").Value = "DATA"; costs.Cell("B1").Value = "DESCRIÇÃO";
            costs.Cell("C1").Value = "VALOR"; costs.Cell("D1").Value = "CATEGORIA";
            costs.Cell("A2").Value = new DateTime(2026, 7, 2); costs.Cell("B2").Value = "Combustível";
            costs.Cell("C2").Value = 150; costs.Cell("D2").Value = "Operacional";
            costs.Cell("A3").Value = new DateTime(2026, 7, 2); costs.Cell("B3").Value = "Combustível";
            costs.Cell("C3").Value = 150; costs.Cell("D3").Value = "Operacional";
            workbook.SaveAs(stream);
        }
        return stream.ToArray();
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
            "Situação", "Descrição da situação", "R$ Contrato", "Data Contrato", "NF", "Condição pag.",
            "A Receber", "Recebido", "Custos" };
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        sheet.Cell("A2").Value = code; sheet.Cell("B2").Value = client; sheet.Cell("C2").Value = "Campinas";
        sheet.Cell("D2").Value = "19999999999"; sheet.Cell("E2").Value = service ?? "";
        sheet.Cell("F2").Value = "Concluído"; sheet.Cell("H2").Value = 1200;
        sheet.Cell("I2").Value = new DateTime(2026, 7, 1); sheet.Cell("K2").Value = "2x";
        sheet.Cell("L2").Value = 800; sheet.Cell("M2").Value = 200; sheet.Cell("N2").Value = 100;
    }
}
