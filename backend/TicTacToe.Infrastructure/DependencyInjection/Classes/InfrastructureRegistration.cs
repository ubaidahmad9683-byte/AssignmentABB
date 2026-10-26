using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Application;
using TicTacToe.Infrastructure.Repository.Classes;
using TicTacToe.Infrastructure.Caching.Classes;
using TicTacToe.Infrastructure;

namespace TicTacToe.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        services.AddDbContext<GameDbContext>(options =>
        {
            if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase(configuration["Database:InMemoryName"] ?? "TicTacToe-Local");
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                return;
            }

            var connectionString = configuration.GetConnectionString("GameDatabase")
                ?? throw new InvalidOperationException("ConnectionStrings:GameDatabase must be configured.");
            options.UseSqlServer(connectionString);
        });
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddMemoryCache();

        if (configuration.GetValue<bool>("Cache:UseRedis"))
        {
            var redis = configuration["Cache:RedisConnection"]
                ?? throw new InvalidOperationException("Cache:RedisConnection is required when Cache:UseRedis is true.");
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = "TicTacToe:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddSingleton<ICacheAside, HybridCacheAside>();
        return services;
    }
}
