using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Servicos;

public sealed class ServicoTipoCampoConfigDto
{
    public long? Id { get; set; }
    public AcompanhamentoServicoTipo TipoServico { get; set; }
    public ServicoCampo Campo { get; set; }
    public bool Visivel { get; set; } = true;
    public bool Obrigatorio { get; set; } = false;
}

public interface IServicoTipoCampoConfigService
{
    Task<IReadOnlyList<ServicoTipoCampoConfigDto>> ListByTipoAsync(AcompanhamentoServicoTipo tipo, CancellationToken ct = default);
    Task<IReadOnlyList<ServicoTipoCampoConfigDto>> ListAsync(CancellationToken ct = default);
    Task SaveAsync(IEnumerable<ServicoTipoCampoConfigDto> configs, CancellationToken ct = default);
    Task<IReadOnlyList<ServicoTipoCampoConfigDto>> GetDefaultsAsync(CancellationToken ct = default);
}

public sealed class ServicoTipoCampoConfigService(IRepository<ServicoTipoCampoConfig> repository, ICrmCache cache)
    : IServicoTipoCampoConfigService
{
    private static string CacheKeyAll => "servico-tipo-campo-config:all";
    private static string CacheKeyByTipo(AcompanhamentoServicoTipo tipo) => $"servico-tipo-campo-config:{tipo}";

    public async Task<IReadOnlyList<ServicoTipoCampoConfigDto>> ListByTipoAsync(
        AcompanhamentoServicoTipo tipo,
        CancellationToken ct = default)
    {
        var key = CacheKeyByTipo(tipo);
        var cached = await cache.GetAsync<IReadOnlyList<ServicoTipoCampoConfigDto>>(key, ct);
        if (cached is not null) return cached;

        var saved = (await repository.ListAsync(ct)).Where(x => x.TipoServico == tipo).ToList();
        var result = MergeWithDefaults(tipo, saved);
        await cache.SetAsync(key, result, TimeSpan.FromHours(1), ct);
        return result;
    }

    public async Task<IReadOnlyList<ServicoTipoCampoConfigDto>> ListAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<IReadOnlyList<ServicoTipoCampoConfigDto>>(CacheKeyAll, ct);
        if (cached is not null) return cached;

        var saved = await repository.ListAsync(ct);
        var result = Enum.GetValues<AcompanhamentoServicoTipo>()
            .SelectMany(t => MergeWithDefaults(t, saved.Where(x => x.TipoServico == t).ToList()))
            .ToList();
        await cache.SetAsync(CacheKeyAll, result, TimeSpan.FromHours(1), ct);
        return result;
    }

    public async Task SaveAsync(IEnumerable<ServicoTipoCampoConfigDto> configs, CancellationToken ct = default)
    {
        var list = configs.ToList();
        var tipos = list.Select(x => x.TipoServico).Distinct().ToList();
        var existing = (await repository.ListAsync(ct)).Where(x => tipos.Contains(x.TipoServico)).ToList();

        foreach (var dto in list)
        {
            var entity = existing.FirstOrDefault(x => x.TipoServico == dto.TipoServico && x.Campo == dto.Campo);
            if (entity is null)
            {
                entity = new ServicoTipoCampoConfig
                {
                    TipoServico = dto.TipoServico,
                    Campo = dto.Campo
                };
                await repository.AddAsync(entity, ct);
            }
            else
            {
                repository.Update(entity);
            }

            entity.Visivel = dto.Visivel;
            entity.Obrigatorio = dto.Obrigatorio;
        }

        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeyAll, ct);
        foreach (var tipo in tipos)
            await cache.RemoveAsync(CacheKeyByTipo(tipo), ct);
    }

    public Task<IReadOnlyList<ServicoTipoCampoConfigDto>> GetDefaultsAsync(CancellationToken ct = default)
    {
        var dtos = Enum.GetValues<AcompanhamentoServicoTipo>()
            .SelectMany(t => DefaultConfigs(t))
            .ToList();
        return Task.FromResult<IReadOnlyList<ServicoTipoCampoConfigDto>>(dtos);
    }

    private static IReadOnlyList<ServicoTipoCampoConfigDto> MergeWithDefaults(
        AcompanhamentoServicoTipo tipo,
        IReadOnlyList<ServicoTipoCampoConfig> saved)
    {
        var defaults = DefaultConfigs(tipo);
        foreach (var d in defaults)
        {
            var s = saved.FirstOrDefault(x => x.Campo == d.Campo);
            if (s is null) continue;
            d.Id = s.Id;
            d.Visivel = s.Visivel;
            d.Obrigatorio = s.Obrigatorio;
        }
        return defaults;
    }

    private static IReadOnlyList<ServicoTipoCampoConfigDto> DefaultConfigs(AcompanhamentoServicoTipo tipo)
    {
        var todos = Enum.GetValues<ServicoCampo>().Select(c => new ServicoTipoCampoConfigDto
        {
            Id = null,
            TipoServico = tipo,
            Campo = c,
            Visivel = true,
            Obrigatorio = false
        }).ToList();

        if (tipo == AcompanhamentoServicoTipo.PROCESSOS_ADM)
        {
            foreach (var c in todos)
            {
                if (c.Campo is ServicoCampo.EnderecoEmpresa or ServicoCampo.EnderecoServico
                    or ServicoCampo.CondicaoPagamento or ServicoCampo.Parcelas or ServicoCampo.Prestadores)
                {
                    c.Visivel = false;
                    c.Obrigatorio = false;
                }
            }
        }

        return todos;
    }
}
