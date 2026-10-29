using StackExchange.Redis;

namespace UCB_Hold.Server.Shared.Caching;

public static class CacheServiceRegistration
{
    public static IServiceCollection AddDistributedCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var settings =
            configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>()
            ?? new RedisSettings();
        var enabled = settings.Enabled ?? !environment.IsDevelopment();

        if (enabled && !string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            var redis = ConfigurationOptions.Parse(settings.ConnectionString);
            redis.ConnectTimeout = settings.ConnectTimeoutMilliseconds;
            redis.SyncTimeout = settings.SyncTimeoutMilliseconds;
            redis.AbortOnConnectFail = false;
            redis.ConnectRetry = settings.ConnectRetry;
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redis;
                options.InstanceName = settings.InstanceName;
            });
        }
        else
            services.AddDistributedMemoryCache();

        return services.AddSingleton<CacheRepository>();
    }
}
