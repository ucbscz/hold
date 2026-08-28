using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class ConfiguracionRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CacheRepository _cache;
    private const string CacheKey = "ConfiguracionGlobal";
    private const int DefaultConfigurationId = 1;

    public ConfiguracionRepository(ApplicationDbContext dbContext, CacheRepository cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<ConfiguracionSistema> GetConfiguracion(
        CancellationToken cancellationToken = default
    )
    {
        var cached = await _cache.Get<ConfiguracionSistema>(CacheKey, cancellationToken);
        if (cached.IsSuccess && cached.Value != null)
        {
            return cached.Value;
        }

        ConfiguracionSistema? config;
        try
        {
            config = await _dbContext.ConfiguracionesSistema.SingleOrDefaultAsync(
                current => current.Id == DefaultConfigurationId,
                cancellationToken
            );
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return ConfiguracionSeed.Default;
        }
        if (config == null)
        {
            config = ConfiguracionSeed.Default;
            _dbContext.ConfiguracionesSistema.Add(config);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _cache.Set(CacheKey, config, TimeSpan.FromDays(1), cancellationToken);
        return config;
    }

    public async Task Update(
        ConfiguracionSistema config,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _dbContext.ConfiguracionesSistema.FirstOrDefaultAsync(
            current => current.Id == config.Id,
            cancellationToken
        );

        if (existing == null)
            throw new KeyNotFoundException("No existe una configuración del sistema para actualizar.");

        _dbContext.Entry(existing).CurrentValues.SetValues(config);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.Remove(CacheKey, cancellationToken);
    }
}
