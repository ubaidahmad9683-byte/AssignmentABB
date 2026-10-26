using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicTacToe.Infrastructure.Caching.Classes;

namespace TicTacToe.Tests;

public sealed class CacheAsideTests
{
    [Fact]
    public async Task UsesDistributedAndLocalCacheUntilInvalidatedAcrossInstances()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        using var provider = services.BuildServiceProvider();
        var distributed = provider.GetRequiredService<IDistributedCache>();
        var logger = provider.GetRequiredService<ILogger<HybridCacheAside>>();
        var first = new HybridCacheAside(provider.GetRequiredService<IMemoryCache>(), distributed, logger);
        using var secondMemory = new MemoryCache(new MemoryCacheOptions());
        var second = new HybridCacheAside(secondMemory, distributed, logger);
        var factoryCalls = 0;
        Task<string> Load(CancellationToken _) => Task.FromResult($"value-{++factoryCalls}");

        Assert.Equal("value-1", await first.GetOrCreateAsync("sample", Load, TimeSpan.FromMinutes(1), default));
        Assert.Equal("value-1", await second.GetOrCreateAsync("sample", Load, TimeSpan.FromMinutes(1), default));
        Assert.Equal(1, factoryCalls);

        await first.RemoveAsync("sample", default);
        Assert.Equal("value-2", await second.GetOrCreateAsync("sample", Load, TimeSpan.FromMinutes(1), default));
        Assert.Equal(2, factoryCalls);
    }
}
