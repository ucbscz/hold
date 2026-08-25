using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class ConfiguracionRepository : IConfiguracionRepository
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
        var existing = await _dbContext.ConfiguracionesSistema.FirstOrDefaultAsync(c => c.Id == config.Id);

        if (existing == null)
            throw new InvalidOperationException("No existe una configuración del sistema para actualizar.");

        _dbContext.Entry(existing).CurrentValues.SetValues(config);
        await _dbContext.SaveChangesAsync();
        await _cache.Remove(CacheKey);
    }
}
