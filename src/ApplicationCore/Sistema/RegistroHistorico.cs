using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Sistema;

public static class RegistroEntidade
{
    public const string Servico = "servico";
    public const string Lancamento = "lancamento";
    public const string Prestador = "prestador";
    public const string Acompanhamento = "acompanhamento";
    public const string Orcamento = "orcamento";
    public const string CustoIndireto = "custo_indireto";

    public static string Label(string entidade) => entidade switch
    {
        Servico => "Serviço",
        Lancamento => "Lançamento",
        Prestador => "Prestador",
        Acompanhamento => "Acompanhamento",
        Orcamento => "Orçamento",
        CustoIndireto => "Custo indireto",
        _ => entidade
    };
}

public sealed class RegistroHistorico : Entity
{
    public string Entidade { get; set; } = string.Empty;
    public long EntidadeId { get; set; }
    public string? EntidadeCodigo { get; set; }
    public string Campo { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorNovo { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record RegistroHistoricoDto(
    long Id,
    string Entidade,
    long EntidadeId,
    string? EntidadeCodigo,
    string Campo,
    string? ValorAnterior,
    string? ValorNovo,
    string? ResponsavelNome,
    DateTime Em);

public static class RegistroHistoricoFormat
{
    public static string? Money(decimal? value) =>
        value?.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    public static string? Data(DateOnly? value) => value?.ToString("dd/MM/yyyy");
    public static string? Data(DateTime? value) => value?.ToString("dd/MM/yyyy");
    public static string Bool(bool? value) => value == true ? "Sim" : "Não";
}

public interface IRegistroHistoricoService
{
    Task RegistrarAsync(
        string entidade,
        long entidadeId,
        string? entidadeCodigo,
        IEnumerable<(string Campo, string? Antes, string? Depois)> campos,
        CancellationToken ct = default);
    Task RegistrarEventoAsync(
        string entidade,
        long entidadeId,
        string? entidadeCodigo,
        string evento,
        CancellationToken ct = default);
    Task<IReadOnlyList<RegistroHistoricoDto>> ListAsync(
        string? entidade = null,
        long? entidadeId = null,
        int take = 200,
        CancellationToken ct = default);
}

public sealed class RegistroHistoricoService(
    IRepository<RegistroHistorico> repository,
    IUserAccessor userAccessor) : IRegistroHistoricoService
{
    public async Task RegistrarAsync(
        string entidade,
        long entidadeId,
        string? entidadeCodigo,
        IEnumerable<(string Campo, string? Antes, string? Depois)> campos,
        CancellationToken ct = default)
    {
        var changes = campos
            .Where(c => !string.Equals(Normalize(c.Antes), Normalize(c.Depois), StringComparison.Ordinal))
            .ToList();
        if (changes.Count == 0) return;

        var responsavel = await userAccessor.GetUserNameAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var (campo, antes, depois) in changes)
        {
            await repository.AddAsync(new RegistroHistorico
            {
                Entidade = entidade,
                EntidadeId = entidadeId,
                EntidadeCodigo = string.IsNullOrWhiteSpace(entidadeCodigo) ? null : entidadeCodigo.Trim(),
                Campo = campo,
                ValorAnterior = antes,
                ValorNovo = depois,
                ResponsavelNome = string.IsNullOrWhiteSpace(responsavel) ? "Sistema" : responsavel.Trim(),
                CreatedAt = now
            }, ct);
        }
        await repository.SaveChangesAsync(ct);
    }

    public Task RegistrarEventoAsync(
        string entidade,
        long entidadeId,
        string? entidadeCodigo,
        string evento,
        CancellationToken ct = default) =>
        RegistrarAsync(entidade, entidadeId, entidadeCodigo, [("Registro", null, evento)], ct);

    public async Task<IReadOnlyList<RegistroHistoricoDto>> ListAsync(
        string? entidade = null,
        long? entidadeId = null,
        int take = 200,
        CancellationToken ct = default)
    {
        var query = repository.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entidade))
            query = query.Where(x => x.Entidade == entidade);
        if (entidadeId is not null)
            query = query.Where(x => x.EntidadeId == entidadeId);
        var items = await repository.ToListAsync(
            query.OrderByDescending(x => x.CreatedAt).Take(Math.Clamp(take, 1, 1000)), ct);
        return items.Select(x => new RegistroHistoricoDto(
            x.Id, x.Entidade, x.EntidadeId, x.EntidadeCodigo, x.Campo,
            x.ValorAnterior, x.ValorNovo, x.ResponsavelNome, x.CreatedAt)).ToList();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
