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
        // Large selectors (for example, linking a financial entry to an older
        // service) must be able to load the complete catalogue.  The previous
        // cap of 100 silently hid valid service codes from the UI.
        pageSize = Math.Clamp(pageSize, 1, 5000);
        var materialized = source.ToList();
        return new(
            materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page,
            pageSize,
            materialized.Count);
    }
}

public sealed class NotFoundException(string message) : Exception(message);
