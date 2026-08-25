using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class ConfiguracionRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CacheRepository _cache;
    private const string CacheKey = "ConfiguracionGlobal";

    public ConfiguracionRepository(ApplicationDbContext dbContext, CacheRepository cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<ConfiguracionSistema> GetConfiguracion()
    {
        var cached = await _cache.Get<ConfiguracionSistema>(CacheKey);
        if (cached.IsSuccess && cached.Value != null)
        {
            return cached.Value;
        }

        var config = await _dbContext.ConfiguracionesSistema.FirstOrDefaultAsync();
        if (config == null)
        {
            config = ConfiguracionSeed.Default;
            _dbContext.ConfiguracionesSistema.Add(config);
            await _dbContext.SaveChangesAsync();
        }

        await _cache.Set(CacheKey, config, TimeSpan.FromDays(1));
        return config;
    }

    public async Task Update(ConfiguracionSistema config)
    {
        _dbContext.ConfiguracionesSistema.Update(config);
        await _dbContext.SaveChangesAsync();
    }

    public async Task InvalidateCache()
    {
        await _cache.Remove(CacheKey);
    }
}
