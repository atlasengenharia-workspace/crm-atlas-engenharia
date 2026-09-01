using System.Text;
using ClosedXML.Excel;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.Infrastructure.Imports;

namespace CrmAtlas.IntegrationTests;

public sealed class AcompanhamentoSpreadsheetReaderTests
{
    private readonly AcompanhamentoSpreadsheetReader _reader = new();

    [Fact]
    public async Task ReadsLegacyCsvAndConvertsExcelSerialDate()
    {
        const string csv = """
            Cod. ;Nome do cliente;Endereço;Telefone;Serviço;Situação;Descrição da situação;R$ Contrato;Data Contrato;NF;Condição pag.
            3100;Cliente Atlas;Campinas;;AVCB;Concluido;Finalizado;2900;45786;Sim;4x
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA(AVCB).csv");

        var row = Assert.Single(rows);
        Assert.True(row.Valido);
        Assert.Equal(AcompanhamentoServicoTipo.AVCB, row.Item!.Tipo);
        Assert.Equal("Cliente Atlas", row.Item.Cliente);
        Assert.Equal("Campinas", row.Item.Endereco);
        Assert.Equal("AVCB", row.Item.Servico);
        Assert.Equal("Sim", row.Item.NotaFiscal);
        Assert.Equal("4x", row.Item.CondicaoPagamento);
        Assert.Equal(2900m, row.Item.ValorContrato);
        Assert.Equal(new DateOnly(2025, 5, 9), row.Item.DataContrato);
    }

    [Fact]
    public async Task ReadsCsvWithNumeroAndCpfCnpjHeaders()
    {
        const string csv = """
            Número;Cliente;CPF/CNPJ;Situação
            4500;Empresa Exemplo;12.345.678/0001-90;Em Andamento
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA(CLCB).csv");

        var row = Assert.Single(rows);
        Assert.True(row.Valido);
        Assert.Equal("4500", row.Codigo);
        Assert.Equal("Empresa Exemplo", row.Item!.Cliente);
        Assert.Equal("12.345.678/0001-90", row.Item.CnpjCpf);
    }

    [Fact]
    public async Task ReadsOnlyRequestedOperationalWorksheet()
    {
        await using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            AddSheet(workbook, "AVCB", "3100", "Cliente AVCB");
            AddSheet(workbook, "OBRAS", "2.02", "Cliente Obras");
            workbook.AddWorksheet("LANÇAMENTOS").Cell("A1").Value = "Ignorar";
            workbook.SaveAs(stream);
        }
        stream.Position = 0;

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA.xlsx", AcompanhamentoServicoTipo.OBRAS);

        var row = Assert.Single(rows);
        Assert.Equal("2.02", row.Codigo);
        Assert.Equal(AcompanhamentoServicoTipo.OBRAS, row.Item!.Tipo);
        Assert.Equal(202, row.Item.OrigemId);
    }

    [Fact]
    public async Task ReadsXlsxFromStreamThatRejectsSynchronousReads()
    {
        await using var workbookBytes = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            AddSheet(workbook, "AVCB", "3101", "Cliente assíncrono");
            workbook.SaveAs(workbookBytes);
        }
        await using var stream = new AsyncOnlyReadStream(workbookBytes.ToArray());

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA.xlsx", AcompanhamentoServicoTipo.AVCB);

        Assert.Single(rows);
        Assert.Equal("3101", rows[0].Codigo);
    }

    [Fact]
    public async Task ReadsFractionalContractValueFromXlsxWithoutChangingItsMagnitude()
    {
        await using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            AddSheet(workbook, "CLCB", "4292", "Patricia - Cosmópolis");
            workbook.Worksheet("CLCB").Cell("E2").Value = 8851.800000000001;
            workbook.SaveAs(stream);
        }
        stream.Position = 0;

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA.xlsx", AcompanhamentoServicoTipo.CLCB);

        var row = Assert.Single(rows);
        Assert.True(row.Valido);
        Assert.NotNull(row.Item);
        Assert.InRange(row.Item.ValorContrato!.Value, 8851.79m, 8851.81m);
    }

    [Fact]
    public async Task UsesFallbackWhenLegacyRowHasNoStatus()
    {
        const string csv = """
            Cod.;Nome do cliente;Situação
            2.84;Marcela - VLJ;
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA(OBRAS).csv");

        var row = Assert.Single(rows);
        Assert.True(row.Valido);
        Assert.Equal("Não informado", row.Item!.Situacao);
    }

    [Fact]
    public async Task ReadsProcessosAdmWithValorContratoAndProximaParcela()
    {
        const string csv = """
            Cod.;Nome do cliente;Serviço;Situação;Descrição da situação;R$ Contrato;Data Contrato;NF;Condição pag.;Prox. Parcela;A Receber;Recebido;Custos
            5001;Cliente Processo;Alvará;Em andamento;Análise documental;4500,50;15/05/2026;Sim;3x;20/06/2026;3000,00;1500,50;200,00
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA(PROCESSOS ADM).csv");

        var row = Assert.Single(rows);
        Assert.True(row.Valido);
        Assert.NotNull(row.Item);
        Assert.Equal(AcompanhamentoServicoTipo.PROCESSOS_ADM, row.Item.Tipo);
        Assert.Equal(4500.50m, row.Item.ValorContrato);
        Assert.Equal(new DateOnly(2026, 5, 15), row.Item.DataContrato);
        Assert.Equal(new DateOnly(2026, 6, 20), row.Item.ProximaParcela);
        Assert.Equal("Alvará", row.Item.Servico);
        Assert.Equal("3x", row.Item.CondicaoPagamento);
    }

    [Fact]
    public async Task ReadsXlsxWithExcelSerialDatesAndAlternativeParcelaHeaders()
    {
        await using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("PROCESSOS ADM");
            sheet.Cell("A1").Value = "Cod.";
            sheet.Cell("B1").Value = "Nome do cliente";
            sheet.Cell("C1").Value = "Situação";
            sheet.Cell("D1").Value = "Descrição da situação";
            sheet.Cell("E1").Value = "R$ Contrato";
            sheet.Cell("F1").Value = "Data Contrato";
            sheet.Cell("G1").Value = "Próxima Parcela";
            sheet.Cell("A2").Value = "5002";
            sheet.Cell("B2").Value = "Cliente Processos Excel";
            sheet.Cell("C2").Value = "Em andamento";
            sheet.Cell("D2").Value = "Análise";
            sheet.Cell("E2").Value = 3500.00;
            // 46183 is 2026-06-10 in Excel OA Date format
            sheet.Cell("F2").Value = 46183;
            // DateTime object in cell
            sheet.Cell("G2").Value = new DateTime(2026, 7, 10);
            workbook.SaveAs(stream);
        }
        stream.Position = 0;

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA.xlsx", AcompanhamentoServicoTipo.PROCESSOS_ADM);

        var row = Assert.Single(rows);
        Assert.True(row.Valido);
        Assert.NotNull(row.Item);
        Assert.Equal(3500m, row.Item.ValorContrato);
        Assert.Equal(new DateOnly(2026, 6, 10), row.Item.DataContrato);
        Assert.Equal(new DateOnly(2026, 7, 10), row.Item.ProximaParcela);
    }

    [Fact]
    public async Task ReadsCsvWithVencimentoOrDtProxHeaders()
    {
        const string csv = """
            Código;Cliente;Situação;Vencimento
            5003;Cliente Teste;Pendente;15/08/2026 00:00:00
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = await _reader.ReadAsync(stream, "PLANILHA PURA(PROCESSOS ADM).csv");

        var row = Assert.Single(rows);
        Assert.True(row.Valido);
        Assert.NotNull(row.Item);
        Assert.Equal(new DateOnly(2026, 8, 15), row.Item.ProximaParcela);
    }

    private static void AddSheet(XLWorkbook workbook, string name, string code, string client)
    {
        var sheet = workbook.AddWorksheet(name);
        sheet.Cell("A1").Value = "Cod.";
        sheet.Cell("B1").Value = "Nome do cliente";
        sheet.Cell("C1").Value = "Situação";
        sheet.Cell("D1").Value = "Descrição da situação";
        sheet.Cell("E1").Value = "R$ Contrato";
        sheet.Cell("F1").Value = "Data Contrato";
        sheet.Cell("A2").Value = code;
        sheet.Cell("B2").Value = client;
        sheet.Cell("C2").Value = "Em andamento";
        sheet.Cell("D2").Value = "Em análise";
        sheet.Cell("E2").Value = 1500m;
        sheet.Cell("F2").Value = new DateTime(2026, 7, 23);
    }

    private sealed class AsyncOnlyReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("Synchronous reads are not supported.");
        public override int Read(Span<byte> buffer) =>
            throw new InvalidOperationException("Synchronous reads are not supported.");
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await _inner.DisposeAsync(); GC.SuppressFinalize(this); }
    }
}
