using LibrarySystem.BLL.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.BLL.Services;

public class MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger) : ICacheService
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<MemoryCacheService> _logger = logger;

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_memoryCache.TryGetValue(key, out T cached))
        {
            _logger.LogInformation("Cache hit: {Key}", key);
            return cached;
        }

        _logger.LogInformation("Cache miss: {Key}. Rebuilding...", key);
        var result = await factory();
        _memoryCache.Set(key, result, expiration ?? TimeSpan.FromMinutes(30));
        return result;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        _logger.LogInformation("Cache invalidated: {Key}", key);
    }

    public void Invalidate(params string[] keys)
    {
        foreach (var key in keys)
        {
            Remove(key);
        }
    }
}
