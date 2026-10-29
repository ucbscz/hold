using FluentValidation;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Infrastructure;

public static class InventoryServiceRegistration
{
    public static IServiceCollection AddInventoryCatalogs(this IServiceCollection services)
    {
        services.AddScoped<Repository<Ambiente, CatalogoInventarioDto>, CatalogoInventarioRepository<Ambiente>>();
        services.AddScoped<Repository<Procedencia, CatalogoInventarioDto>, CatalogoInventarioRepository<Procedencia>>();
        services.AddScoped<Service<Ambiente, Repository<Ambiente, CatalogoInventarioDto>, CatalogoInventarioDto>>();
        services.AddScoped<Service<Procedencia, Repository<Procedencia, CatalogoInventarioDto>, CatalogoInventarioDto>>();
        services.AddScoped<IMapper<Ambiente, CatalogoInventarioDto>, AmbienteMapper>();
        services.AddScoped<IMapper<Procedencia, CatalogoInventarioDto>, ProcedenciaMapper>();
        services.AddScoped<IValidator<CatalogoInventarioDto>, CatalogoInventarioValidator>();
        return services;
    }
}
