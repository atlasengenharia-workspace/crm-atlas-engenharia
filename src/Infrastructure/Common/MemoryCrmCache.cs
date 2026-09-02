using CrmAtlas.ApplicationCore.Common;
using Microsoft.Extensions.Caching.Memory;

namespace CrmAtlas.Infrastructure.Common;

public sealed class MemoryCrmCache(IMemoryCache cache) : ICrmCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        else
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

        cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}
