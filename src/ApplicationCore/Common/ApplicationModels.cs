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
        pageSize = Math.Clamp(pageSize, 1, 5000);
        var materialized = source.ToList();
        return new(
            materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page,
            pageSize,
            materialized.Count);
    }
}

public sealed record CursorResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long? NextCursor,
    bool HasNext);

public static class CursorPagination
{
    public static int ClampPageSize(int pageSize) => Math.Clamp(pageSize, 1, 5000);

    public static CursorResult<T> Create<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        bool hasNext)
    {
        pageSize = ClampPageSize(pageSize);
        var nextCursor = hasNext && items.Count > 0 ? (items[items.Count - 1] as dynamic)?.Id : null;
        var paged = hasNext ? items.Take(pageSize).ToList() : items;
        return new CursorResult<T>(paged, page, pageSize, nextCursor, hasNext);
    }
}

public sealed class NotFoundException(string message) : Exception(message);
