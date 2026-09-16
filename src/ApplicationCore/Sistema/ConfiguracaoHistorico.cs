using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Sistema;

public static class ConfiguracaoContexto
{
    public const string SituacaoAcompanhamento = "situacao-acompanhamento";
    public const string SituacaoOrcamento = "situacao-orcamento";

    public static string Label(string contexto) => contexto switch
    {
        SituacaoAcompanhamento => "Situações de acompanhamento",
        SituacaoOrcamento => "Situações de orçamento",
        _ => contexto
    };
}

public sealed class ConfiguracaoHistorico : Entity
{
    public string Contexto { get; set; } = string.Empty;
    public string? Escopo { get; set; }
    public long? RegistroId { get; set; }
    public string RegistroNome { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string? Detalhes { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record ConfiguracaoHistoricoDto(
    long Id,
    string Contexto,
    string? Escopo,
    long? RegistroId,
    string RegistroNome,
    string Acao,
    string? Detalhes,
    string? ResponsavelNome,
    DateTime Em);

public static class ConfiguracaoDiff
{
    public static string? Between(params (string Campo, string? Antes, string? Depois)[] campos)
    {
        var changes = campos
            .Where(c => !string.Equals(c.Antes, c.Depois, StringComparison.Ordinal))
            .Select(c => $"{c.Campo}: {c.Antes ?? "—"} → {c.Depois ?? "—"}")
            .ToList();
        return changes.Count == 0 ? null : string.Join("; ", changes);
    }
}

public interface IConfiguracaoHistoricoService
{
    Task RegistrarAsync(
        string contexto,
        string? escopo,
        long? registroId,
        string registroNome,
        string acao,
        string? detalhes = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfiguracaoHistoricoDto>> ListAsync(
        string? contexto = null,
        int take = 200,
        CancellationToken cancellationToken = default);
}

public sealed class ConfiguracaoHistoricoService(
    IRepository<ConfiguracaoHistorico> repository,
    IUserAccessor userAccessor) : IConfiguracaoHistoricoService
{
    public async Task RegistrarAsync(
        string contexto,
        string? escopo,
        long? registroId,
        string registroNome,
        string acao,
        string? detalhes = null,
        CancellationToken cancellationToken = default)
    {
        var responsavel = await userAccessor.GetUserNameAsync(cancellationToken);
        await repository.AddAsync(new ConfiguracaoHistorico
        {
            Contexto = contexto,
            Escopo = string.IsNullOrWhiteSpace(escopo) ? null : escopo.Trim(),
            RegistroId = registroId,
            RegistroNome = registroNome.Trim(),
            Acao = acao,
            Detalhes = string.IsNullOrWhiteSpace(detalhes) ? null : detalhes,
            ResponsavelNome = string.IsNullOrWhiteSpace(responsavel) ? "Sistema" : responsavel.Trim(),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfiguracaoHistoricoDto>> ListAsync(
        string? contexto = null,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var query = repository.AsQueryable();
        if (!string.IsNullOrWhiteSpace(contexto))
            query = query.Where(x => x.Contexto == contexto);
        var items = await repository.ToListAsync(
            query.OrderByDescending(x => x.CreatedAt).Take(Math.Clamp(take, 1, 1000)), cancellationToken);
        return items.Select(x => new ConfiguracaoHistoricoDto(
            x.Id, x.Contexto, x.Escopo, x.RegistroId, x.RegistroNome, x.Acao, x.Detalhes, x.ResponsavelNome, x.CreatedAt)).ToList();
    }
}
