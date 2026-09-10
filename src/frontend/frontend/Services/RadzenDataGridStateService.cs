using Microsoft.JSInterop;
using Radzen;
using System.Text.Json;

namespace CrmAtlas.Web.Services;

public interface IRadzenDataGridStateService
{
    ValueTask<DataGridSettings?> LoadAsync(string key, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(string key, DataGridSettings settings, CancellationToken cancellationToken = default);
}

public sealed class RadzenDataGridStateService(IJSRuntime jsRuntime) : IRadzenDataGridStateService
{
    private static string StorageKey(string key) => $"atlas.datagrid.{key}";

    public async ValueTask<DataGridSettings?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, StorageKey(key));
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<DataGridSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask SaveAsync(string key, DataGridSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, StorageKey(key), json);
        }
        catch
        {
            // Ignora falhas de storage para nao travar a UI
        }
    }
}
