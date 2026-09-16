using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Servicos;
using CrmAtlas.ApplicationCore.Sistema;

namespace CrmAtlas.ApplicationCore.Operacao;

public sealed record OrcamentoSituacaoDto(
    long? Id,
    string Nome,
    int Ordem,
    bool Padrao,
    bool Ativo,
    bool Fechado,
    string? Cor);

public interface IOrcamentoSituacaoService
{
    Task<IReadOnlyList<OrcamentoSituacaoDto>> ListAsync(CancellationToken ct = default);
    Task<OrcamentoSituacaoDto> SaveAsync(OrcamentoSituacaoDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public sealed class OrcamentoSituacaoService(
    IRepository<OrcamentoSituacao> repository,
    IConfiguracaoHistoricoService historico,
    ICrmCache cache) : IOrcamentoSituacaoService
{
    private const string CacheKey = "orcamento-situacao:all";

    public async Task<IReadOnlyList<OrcamentoSituacaoDto>> ListAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<IReadOnlyList<OrcamentoSituacaoDto>>(CacheKey, ct);
        if (cached is not null) return cached;
        var items = (await repository.ListAsync(ct))
            .OrderBy(x => x.Ordem)
            .Select(Map)
            .ToList();
        await cache.SetAsync(CacheKey, (IReadOnlyList<OrcamentoSituacaoDto>)items, TimeSpan.FromHours(1), ct);
        return items;
    }

    public async Task<OrcamentoSituacaoDto> SaveAsync(OrcamentoSituacaoDto dto, CancellationToken ct = default)
    {
        var nome = string.IsNullOrWhiteSpace(dto.Nome)
            ? throw new ArgumentException("O nome da situação é obrigatório.")
            : dto.Nome.Trim();

        var all = await repository.ListAsync(ct);
        if (all.Any(x => x.Id != dto.Id && x.Label.Equals(nome, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Já existe uma situação de orçamento com esse nome.");

        var isNew = dto.Id is null;
        var entity = isNew
            ? new OrcamentoSituacao { CreatedAt = DateTime.UtcNow }
            : all.FirstOrDefault(x => x.Id == dto.Id) ?? throw new NotFoundException("Situação não encontrada.");

        var detalhes = isNew ? null : ConfiguracaoDiff.Between(
            ("Nome", entity.Label, nome),
            ("Ordem", entity.Ordem.ToString(), dto.Ordem.ToString()),
            ("Padrão", Bool(entity.Padrao), Bool(dto.Padrao)),
            ("Ativa", Bool(entity.Ativo), Bool(dto.Ativo)),
            ("Encerra orçamento", Bool(entity.Closed), Bool(dto.Fechado)),
            ("Cor", entity.Cor, dto.Cor?.Trim()));

        entity.Label = nome;
        entity.Ordem = dto.Ordem;
        entity.Padrao = dto.Padrao;
        entity.Ativo = dto.Ativo;
        entity.Closed = dto.Fechado;
        entity.Cor = string.IsNullOrWhiteSpace(dto.Cor) ? null : dto.Cor.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        if (entity.Padrao)
            foreach (var other in all.Where(x => x.Id != entity.Id && x.Padrao))
            {
                other.Padrao = false;
                repository.Update(other);
            }

        if (isNew) await repository.AddAsync(entity, ct);
        else repository.Update(entity);
        await repository.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey, ct);

        await historico.RegistrarAsync(
            ConfiguracaoContexto.SituacaoOrcamento, "Orçamentos", entity.Id, entity.Label,
            isNew ? "Criada" : "Atualizada", isNew ? null : detalhes, ct);
        return Map(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Situação não encontrada.");
        repository.Remove(entity);
        await repository.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey, ct);
        await historico.RegistrarAsync(
            ConfiguracaoContexto.SituacaoOrcamento, "Orçamentos", entity.Id, entity.Label, "Excluída", null, ct);
    }

    private static string Bool(bool value) => value ? "Sim" : "Não";

    private static OrcamentoSituacaoDto Map(OrcamentoSituacao x) =>
        new(x.Id, x.Label, x.Ordem, x.Padrao, x.Ativo, x.Closed, x.Cor);
}
