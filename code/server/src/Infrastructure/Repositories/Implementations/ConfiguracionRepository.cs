using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class ConfiguracionRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CacheRepository _cache;
    private const string CacheKey = "ConfiguracionGlobal:v2";
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

    public Task<Usuario?> GetResponsable(string? carnet, CancellationToken cancellationToken) =>
        UsuariosDisponibles()
            .Where(u => carnet == null ? u.Rol == TipoUsuario.Administrador : u.Carnet == carnet)
            .OrderBy(u => u.Carnet)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<Usuario>> BuscarResponsables(string? buscar, CancellationToken cancellationToken)
    {
        var query = UsuariosDisponibles();
        var terminos = (buscar ?? string.Empty).Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(8);
        foreach (var termino in terminos)
            query = query.Where(u => (u.Nombre + " " + u.ApellidoPaterno + " " + u.ApellidoMaterno)
                .ToLower().Contains(termino));

        return query.OrderBy(u => u.Nombre).ThenBy(u => u.ApellidoPaterno).ThenBy(u => u.Carnet)
            .Take(30).ToListAsync(cancellationToken);
    }

    private IQueryable<Usuario> UsuariosDisponibles() => _dbContext.Usuarios.AsNoTracking()
        .Where(u => !u.Bloqueado)
        .Select(u => new Usuario
        {
            Carnet = u.Carnet,
            Nombre = u.Nombre,
            ApellidoPaterno = u.ApellidoPaterno,
            ApellidoMaterno = u.ApellidoMaterno,
            Rol = u.Rol
        });
}
