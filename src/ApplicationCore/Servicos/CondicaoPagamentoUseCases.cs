using System.ComponentModel.DataAnnotations;
using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Servicos;

public sealed record CondicaoPagamentoDto(
    long? Id,
    [Required] string Nome,
    [Range(1, int.MaxValue)] int QuantidadeParcelas,
    int? IntervaloDias,
    bool Indefinido);

public sealed record CondicaoPagamentoFilter(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    long? AfterId = null);

public interface ICondicaoPagamentoService
{
    Task<CursorResult<CondicaoPagamentoDto>> ListAsync(CondicaoPagamentoFilter? filter = null, CancellationToken cancellationToken = default);
    Task<CondicaoPagamentoDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<CondicaoPagamentoDto> CreateAsync(CondicaoPagamentoDto dto, CancellationToken cancellationToken = default);
    Task<CondicaoPagamentoDto> UpdateAsync(long id, CondicaoPagamentoDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class CondicaoPagamentoService(IRepository<CondicaoPagamento> repository)
    : ICondicaoPagamentoService
{
    public async Task<CursorResult<CondicaoPagamentoDto>> ListAsync(CondicaoPagamentoFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = repository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Search))
            query = query.Where(x => x.Nome.Contains(filter.Search.Trim()));

        if (filter?.AfterId is not null)
            query = query.Where(x => x.Id < filter.AfterId);

        query = query.OrderByDescending(x => x.Id);

        var pageSize = CursorPagination.ClampPageSize(filter?.PageSize ?? 20);
        var items = await repository.ToListAsync(query.Take(pageSize + 1), cancellationToken);
        var hasNext = items.Count > pageSize;
        var nextCursor = hasNext ? items[pageSize - 1].Id : (long?)null;
        var dtos = items.Take(pageSize).Select(ToDto).ToList();

        return new CursorResult<CondicaoPagamentoDto>(dtos, filter?.Page ?? 1, pageSize, nextCursor, hasNext);
    }

    public async Task<CondicaoPagamentoDto> GetAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await FindAsync(id, cancellationToken));

    public async Task<CondicaoPagamentoDto> CreateAsync(
        CondicaoPagamentoDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = new CondicaoPagamento();
        await ApplyAsync(entity, dto, null, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CondicaoPagamentoDto> UpdateAsync(
        long id,
        CondicaoPagamentoDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        await ApplyAsync(entity, dto, id, cancellationToken);
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAsync(
        CondicaoPagamento entity,
        CondicaoPagamentoDto dto,
        long? currentId,
        CancellationToken cancellationToken)
    {
        var nome = string.IsNullOrWhiteSpace(dto.Nome)
            ? throw new ArgumentException("O nome da condição de pagamento é obrigatório.")
            : dto.Nome.Trim();
        if (dto.QuantidadeParcelas < 1)
            throw new ArgumentException("A quantidade de parcelas deve ser maior que zero.");
        int? intervalo = dto.Indefinido ? null : dto.IntervaloDias ?? 30;
        if (intervalo is < 1)
            throw new ArgumentException("O intervalo entre parcelas deve ser maior que zero.");
        var all = await repository.ListAsync(cancellationToken);
        if (all.Any(x => x.Id != currentId && x.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Já existe uma condição de pagamento com esse nome.");

        entity.Nome = nome;
        entity.QuantidadeParcelas = dto.QuantidadeParcelas;
        entity.IntervaloDias = intervalo;
        entity.Indefinido = dto.Indefinido;
    }

    private async Task<CondicaoPagamento> FindAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Condição de pagamento não encontrada com id: {id}.");

    private static CondicaoPagamentoDto ToDto(CondicaoPagamento x) =>
        new(x.Id, x.Nome, x.QuantidadeParcelas ?? 1, x.IntervaloDias, x.Indefinido);
}
