namespace CrmAtlas.ApplicationCore.Sistema;

public enum CategoriaAtualizacao
{
    NovoRecurso,
    Melhoria,
    Correcao,
    Seguranca
}

public sealed record SistemaAtualizacaoItem(
    string Titulo,
    string? Detalhes = null);

public sealed record SistemaAtualizacao(
    string Versao,
    DateOnly Data,
    string Titulo,
    string Descricao,
    CategoriaAtualizacao Categoria,
    IReadOnlyList<SistemaAtualizacaoItem> Destaques,
    bool DestaquePrincipal = false);

public interface ISistemaAtualizacaoService
{
    Task<IReadOnlyList<SistemaAtualizacao>> GetListAsync(CancellationToken cancellationToken = default);
    Task<SistemaAtualizacao?> GetLatestAsync(CancellationToken cancellationToken = default);
}
