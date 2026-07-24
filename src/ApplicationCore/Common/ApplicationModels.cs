namespace CrmAtlas.ApplicationCore.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public static PagedResult<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var materialized = source.ToList();
        return new(
            materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page,
            pageSize,
            materialized.Count);
    }
}

public sealed class NotFoundException(string message) : Exception(message);

