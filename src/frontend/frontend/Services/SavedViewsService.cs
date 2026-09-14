using Microsoft.JSInterop;
using System.Text.Json;

namespace CrmAtlas.Web.Services;

public sealed record SavedView(string Name, string StateJson, DateTime SavedAtUtc);

public interface ISavedViewsService
{
    ValueTask<IReadOnlyList<SavedView>> ListAsync(string pageKey, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(string pageKey, SavedView view, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string pageKey, string name, CancellationToken cancellationToken = default);
}

public sealed class SavedViewsService(IJSRuntime jsRuntime) : ISavedViewsService
{
    private static string StorageKey(string pageKey) => $"atlas.savedviews.{pageKey}";

    public async ValueTask<IReadOnlyList<SavedView>> ListAsync(string pageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, StorageKey(pageKey));
            if (string.IsNullOrWhiteSpace(json)) return [];
            return JsonSerializer.Deserialize<List<SavedView>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async ValueTask SaveAsync(string pageKey, SavedView view, CancellationToken cancellationToken = default)
    {
        var views = (await ListAsync(pageKey, cancellationToken)).ToList();
        views.RemoveAll(x => string.Equals(x.Name, view.Name, StringComparison.OrdinalIgnoreCase));
        views.Add(view);
        await PersistAsync(pageKey, views, cancellationToken);
    }

    public async ValueTask DeleteAsync(string pageKey, string name, CancellationToken cancellationToken = default)
    {
        var views = (await ListAsync(pageKey, cancellationToken))
            .Where(x => !string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        await PersistAsync(pageKey, views, cancellationToken);
    }

    private async ValueTask PersistAsync(string pageKey, List<SavedView> views, CancellationToken cancellationToken)
    {
        try
        {
            var ordered = views.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var json = JsonSerializer.Serialize(ordered);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, StorageKey(pageKey), json);
        }
        catch
        {
            // Ignora falhas de storage para nao travar a UI
        }
    }
}
