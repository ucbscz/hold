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

    public async Task<Result<T>> Get<T>(
        string cacheKey,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (cachedJson is null)
                return Result<T>.NotFound();

            var cachedValue = JsonSerializer.Deserialize<T>(cachedJson);

            return cachedValue is null ? Result<T>.NotFound() : Result<T>.Success(cachedValue);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo leer la clave de caché {CacheKey}", cacheKey);
            return Result<T>.NotFound();
        }
    }

    public async Task<Result> Set<T>(
        string cacheKey,
        T value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeToLive,
            };
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(value),
                entryOptions,
                cancellationToken
            );

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo escribir la clave de caché {CacheKey}", cacheKey);
            return Result.Error("Caché no disponible");
        }
    }

    public async Task<Result> Remove(
        string cacheKey,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo invalidar la clave de caché {CacheKey}", cacheKey);
            return Result.Error("Caché no disponible");
        }
    }
}
