using System.Text.Json;
using Ardalis.Result;
using Microsoft.Extensions.Caching.Distributed;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class CacheRepository
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheRepository> _logger;

    public CacheRepository(IDistributedCache cache, ILogger<CacheRepository> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<T>> Get<T>(string cacheKey)
    {
        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey);

            if (cachedJson is null)
                return Result<T>.NotFound();

            var cachedValue = JsonSerializer.Deserialize<T>(cachedJson);

            return cachedValue is null ? Result<T>.NotFound() : Result<T>.Success(cachedValue);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo leer la clave de caché {CacheKey}", cacheKey);
            return Result<T>.NotFound();
        }
    }

    public async Task<Result> Set<T>(string cacheKey, T value, TimeSpan timeToLive)
    {
        try
        {
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeToLive,
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(value), entryOptions);

            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo escribir la clave de caché {CacheKey}", cacheKey);
            return Result.Error("Caché no disponible");
        }
    }

    public async Task<Result> Remove(string cacheKey)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey);

            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo invalidar la clave de caché {CacheKey}", cacheKey);
            return Result.Error("Caché no disponible");
        }
    }
}
