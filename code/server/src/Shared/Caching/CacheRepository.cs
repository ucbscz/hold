using System.Text.Json;
using Ardalis.Result;
using Microsoft.Extensions.Caching.Distributed;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Shared.Caching;

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
            _logger.CacheReadFailed(exception);
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
            _logger.CacheWriteFailed(exception);
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
            _logger.CacheRemoveFailed(exception);
            return Result.Error("Caché no disponible");
        }
    }
}
