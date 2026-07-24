namespace CrmAtlas.ApplicationCore.Operacao;

public sealed record AtlasWorkbookImportResult(
    int ClientesCriados,
    int CondicoesPagamentoCriadas,
    int ServicosCriados,
    int AcompanhamentosCriados,
    int LancamentosCriados,
    int CustosIndiretosCriados,
    int RegistrosIgnorados);

public interface IAtlasWorkbookImportService
{
    Task<AtlasWorkbookImportResult> ImportAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default);
}
