namespace CrmAtlas.Web.Components.Shared;

public sealed record SmartFilterColumn<TItem>(
    string Key,
    string Label,
    Func<TItem, object?> Value,
    int? DisplayIndex = null,
    bool Summable = false);
