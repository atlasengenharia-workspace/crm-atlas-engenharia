using System.ComponentModel.DataAnnotations;
using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Servicos;

public static class FormaPagamentoDefaults
{
    public static readonly string[] Nomes = ["PIX", "Boleto", "Transferência", "Cartão"];
}

public sealed record FormaPagamentoDto(
    long? Id,
    [Required] string Nome);

public sealed record FormaPagamentoFilter(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? SortKey = null,
    bool SortDescending = false);

public interface IFormaPagamentoService
{
    Task<PagedResult<FormaPagamentoDto>> ListAsync(FormaPagamentoFilter? filter = null, CancellationToken cancellationToken = default);
    Task<FormaPagamentoDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<FormaPagamentoDto> CreateAsync(FormaPagamentoDto dto, CancellationToken cancellationToken = default);
    Task<FormaPagamentoDto> UpdateAsync(long id, FormaPagamentoDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class FormaPagamentoService(IRepository<FormaPagamento> repository, ICrmCache cache)
    : IFormaPagamentoService
{
    private const string CacheKey = "forma-pagamento:all";

    public async Task<PagedResult<FormaPagamentoDto>> ListAsync(FormaPagamentoFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var cacheable = filter is null || (filter.PageSize == 0 && string.IsNullOrWhiteSpace(filter.Search));
        if (cacheable)
        {
            var cached = await cache.GetAsync<PagedResult<FormaPagamentoDto>>(CacheKey, cancellationToken);
            if (cached is not null) return cached;
        }

        var query = repository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Search))
            query = query.Where(x => x.Nome.Contains(filter.Search.Trim()));

        var ordered = string.Equals(filter?.SortKey, "nome", StringComparison.OrdinalIgnoreCase)
            ? (filter?.SortDescending ?? false ? query.OrderByDescending(x => x.Nome) : query.OrderBy(x => x.Nome))
            : (filter?.SortDescending ?? false ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id));
        query = ordered.ThenBy(x => x.Id);

        var all = filter?.PageSize == 0;
        var pageSize = all ? 0 : CursorPagination.ClampPageSize(filter?.PageSize ?? 20);
        var page = Math.Max(1, filter?.Page ?? 1);
        var total = await repository.CountAsync(query, cancellationToken);
        var items = all
            ? await repository.ToListAsync(query, cancellationToken)
            : await repository.ToListAsync(query.Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);
        var dtos = items.Select(x => new FormaPagamentoDto(x.Id, x.Nome)).ToList();

        var result = PagedResult<FormaPagamentoDto>.Create(dtos, page, all ? total : pageSize, total);
        if (cacheable)
            await cache.SetAsync(CacheKey, result, TimeSpan.FromHours(1), cancellationToken);
        return result;
    }

    public async Task<FormaPagamentoDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        return new FormaPagamentoDto(entity.Id, entity.Nome);
    }

    public async Task<FormaPagamentoDto> CreateAsync(FormaPagamentoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new FormaPagamento();
        entity.Nome = await ValidatedNomeAsync(dto.Nome, null, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKey, cancellationToken);
        return new FormaPagamentoDto(entity.Id, entity.Nome);
    }

    public async Task<FormaPagamentoDto> UpdateAsync(long id, FormaPagamentoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        entity.Nome = await ValidatedNomeAsync(dto.Nome, id, cancellationToken);
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKey, cancellationToken);
        return new FormaPagamentoDto(entity.Id, entity.Nome);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKey, cancellationToken);
    }

    private async Task<string> ValidatedNomeAsync(string? nome, long? currentId, CancellationToken cancellationToken)
    {
        var trimmed = string.IsNullOrWhiteSpace(nome)
            ? throw new ArgumentException("O nome da forma de pagamento é obrigatório.")
            : nome.Trim();
        var all = await repository.ListAsync(cancellationToken);
        if (all.Any(x => x.Id != currentId && x.Nome.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Já existe uma forma de pagamento com esse nome.");
        return trimmed;
    }

    private async Task<FormaPagamento> FindAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Forma de pagamento não encontrada com id: {id}.");
}
