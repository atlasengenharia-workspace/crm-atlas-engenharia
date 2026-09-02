using CrmAtlas.ApplicationCore.Common;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CrmAtlas.Infrastructure.Common;

public sealed class RedisCrmCache(IDistributedCache cache) : ICrmCache
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var data = await cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(data)) return null;
        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
        };
        var json = JsonSerializer.Serialize(value);
        await cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return cache.RemoveAsync(key, cancellationToken);
    }
}
