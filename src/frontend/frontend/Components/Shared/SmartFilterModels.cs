namespace CrmAtlas.Web.Components.Shared;

public sealed record SmartFilterColumn<TItem>(
    string Key,
    string Label,
    Func<TItem, object?> Value,
    int? DisplayIndex = null,
    bool Summable = false);

public sealed record SmartTableState(
    string? Search,
    string? SortKey,
    bool SortDescending,
    int Density,
    IReadOnlySet<string> HiddenColumns,
    IReadOnlyList<ActiveFilter> Filters);

public sealed record ActiveFilter(string ColumnKey, SmartFilterOperator Operator, string Value);

public enum SmartFilterOperator { Contains, Equals, NotEquals, StartsWith, IsEmpty, IsNotEmpty }
