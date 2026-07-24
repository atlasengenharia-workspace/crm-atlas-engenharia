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
