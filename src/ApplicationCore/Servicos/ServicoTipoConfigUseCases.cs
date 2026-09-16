using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Servicos;

public static class ServicoTipoDefaults
{
    public static string Label(AcompanhamentoServicoTipo tipo) => tipo switch
    {
        AcompanhamentoServicoTipo.OBRAS => "Obras",
        AcompanhamentoServicoTipo.PROCESSOS_ADM => "Processos administrativos",
        _ => tipo.ToString()
    };

    public static IReadOnlyList<ServicoTipoConfigDto> All() =>
        Enum.GetValues<AcompanhamentoServicoTipo>()
            .Select((tipo, index) => new ServicoTipoConfigDto(null, tipo, null, index, true))
            .ToList();
}

public sealed record ServicoTipoConfigDto(
    long? Id,
    AcompanhamentoServicoTipo TipoServico,
    string? Nome,
    int Ordem,
    bool Ativo)
{
    public string Label => string.IsNullOrWhiteSpace(Nome) ? ServicoTipoDefaults.Label(TipoServico) : Nome;
}

public interface IServicoTipoConfigService
{
    Task<IReadOnlyList<ServicoTipoConfigDto>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServicoTipoConfigDto>> ListActiveAsync(CancellationToken ct = default);
    Task SaveAsync(IReadOnlyList<ServicoTipoConfigDto> configs, CancellationToken ct = default);
}

public sealed class ServicoTipoConfigService(IRepository<ServicoTipoConfig> repository, ICrmCache cache)
    : IServicoTipoConfigService
{
    private const string CacheKeyAll = "servico-tipo-config:all";

    public async Task<IReadOnlyList<ServicoTipoConfigDto>> ListAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<IReadOnlyList<ServicoTipoConfigDto>>(CacheKeyAll, ct);
        if (cached is not null) return cached;

        var saved = await repository.ListAsync(ct);
        var result = MergeWithDefaults(saved);
        await cache.SetAsync(CacheKeyAll, result, TimeSpan.FromHours(1), ct);
        return result;
    }

    public async Task<IReadOnlyList<ServicoTipoConfigDto>> ListActiveAsync(CancellationToken ct = default) =>
        (await ListAsync(ct)).Where(x => x.Ativo).ToList();

    public async Task SaveAsync(IReadOnlyList<ServicoTipoConfigDto> configs, CancellationToken ct = default)
    {
        var list = configs.Where(x => Enum.IsDefined(x.TipoServico)).ToList();
        if (list.Count == 0 || list.All(x => !x.Ativo))
            throw new ArgumentException("Ao menos um tipo de serviço precisa permanecer ativo.");

        var existing = await repository.ListAsync(ct);
        foreach (var dto in list)
        {
            var entity = existing.FirstOrDefault(x => x.TipoServico == dto.TipoServico);
            if (entity is null)
            {
                entity = new ServicoTipoConfig { TipoServico = dto.TipoServico };
                await repository.AddAsync(entity, ct);
            }
            else
            {
                repository.Update(entity);
            }

            entity.Nome = string.IsNullOrWhiteSpace(dto.Nome) ? null : dto.Nome.Trim();
            entity.Ordem = dto.Ordem;
            entity.Ativo = dto.Ativo;
        }

        await repository.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeyAll, ct);
    }

    private static IReadOnlyList<ServicoTipoConfigDto> MergeWithDefaults(IReadOnlyList<ServicoTipoConfig> saved) =>
        Enum.GetValues<AcompanhamentoServicoTipo>()
            .Select((tipo, index) =>
            {
                var config = saved.FirstOrDefault(x => x.TipoServico == tipo);
                return new ServicoTipoConfigDto(
                    config?.Id,
                    tipo,
                    config?.Nome,
                    config?.Ordem ?? index,
                    config?.Ativo ?? true);
            })
            .OrderBy(x => x.Ordem)
            .ToList();
}
