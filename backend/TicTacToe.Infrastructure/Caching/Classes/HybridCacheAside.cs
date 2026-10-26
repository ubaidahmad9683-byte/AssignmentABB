using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicTacToe.Application;

namespace TicTacToe.Infrastructure.Caching.Classes;

public sealed class HybridCacheAside(
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    ILogger<HybridCacheAside> logger) : ICacheAside
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan LocalLifetime = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan InvalidatedVersionLifetime = TimeSpan.FromMinutes(10);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        var versionKey = $"{key}:version";
        try
        {
            var version = await distributedCache.GetStringAsync(versionKey, cancellationToken);
            if (version is not null &&
                memoryCache.TryGetValue(key, out CacheEntry<T>? localEntry) &&
                localEntry is not null &&
                localEntry.Version == version)
                return localEntry.Value;

            var bytes = await distributedCache.GetAsync(key, cancellationToken);
            if (bytes is not null)
            {
                var envelope = JsonSerializer.Deserialize<CacheEnvelope<T>>(bytes, JsonOptions);
                if (envelope is not null && envelope.Version == version)
                {
                    memoryCache.Set(key, new CacheEntry<T>(version!, envelope.Value), LocalLifetime);
                    return envelope.Value;
                }
            }
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Distributed cache unavailable while reading {CacheKey}; using database fallback", key);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Distributed cache timed out while reading {CacheKey}; using database fallback", key);
        }

        var created = await factory(cancellationToken);
        if (created is null) return created;
        var newVersion = Guid.NewGuid().ToString("N");
        memoryCache.Set(key, new CacheEntry<T>(newVersion, created), LocalLifetime);
        try
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime };
            await distributedCache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(
                new CacheEnvelope<T>(newVersion, created), JsonOptions), options, cancellationToken);
            await distributedCache.SetStringAsync(versionKey, newVersion, options, cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Distributed cache unavailable while writing {CacheKey}", key);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Distributed cache timed out while writing {CacheKey}", key);
        }
        return created;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        memoryCache.Remove(key);
        try
        {
            await distributedCache.SetStringAsync($"{key}:version", Guid.NewGuid().ToString("N"),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = InvalidatedVersionLifetime }, cancellationToken);
            await distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Distributed cache unavailable while invalidating {CacheKey}", key);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Distributed cache timed out while invalidating {CacheKey}", key);
        }
    }

    private sealed record CacheEnvelope<TValue>(string Version, TValue Value);
    private sealed record CacheEntry<TValue>(string Version, TValue Value);
}
