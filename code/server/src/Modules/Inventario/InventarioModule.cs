using FluentValidation;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Modules.Inventario.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Search;

namespace UCB_Hold.Server.Modules.Inventario;

public static class InventarioModule
{
    public static IServiceCollection AddInventarioModule(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<EquipmentLabels>();
        services.AddInventoryCatalogs();

        services.AddScoped<IAuditEntityNames, InventarioAuditNames>();
        services.AddScoped<EquipoRepository>();
        services.AddScoped<AccesorioRepository>();
        services.AddScoped<GrupoEquipoRepository>();
        services.AddScoped<GaveteroRepository>();
        services.AddScoped<ComponenteRepository>();
        services.AddScoped<ComentarioEquipoRepository>();
        services.AddScoped<Repository<Categoria, CategoriaDto>>();
        services.AddScoped<Repository<Mueble, MuebleDto>>();

        services.AddScoped<EquipoService>();
        services.AddScoped<GrupoEquipoService>();
        services.AddSingleton<GrupoEquipoImportacionService>();
        services.AddScoped<GaveteroService>();
        services.AddScoped<ComentarioEquipoService>();
        services.AddScoped<Service<Categoria, Repository<Categoria, CategoriaDto>, CategoriaDto>>();
        services.AddScoped<Service<Mueble, Repository<Mueble, MuebleDto>, MuebleDto>>();
        services.AddScoped<Service<Accesorio, AccesorioRepository, AccesorioDto>>();
        services.AddScoped<Service<Componente, ComponenteRepository, ComponenteDto>>();

        services.AddMapper<Equipo, EquipoDto, EquipoMapper>();
        services.AddMapper<Categoria, CategoriaDto, CategoriaMapper>();
        services.AddMapper<GrupoEquipo, GrupoEquipoDto, GrupoEquipoMapper>();
        services.AddMapper<Gavetero, GaveteroDto, GaveteroMapper>();
        services.AddMapper<Mueble, MuebleDto, MuebleMapper>();
        services.AddMapper<Accesorio, AccesorioDto, AccesorioMapper>();
        services.AddMapper<Componente, ComponenteDto, ComponenteMapper>();

        services.AddScoped<IValidator<AccesorioDto>, AccesorioValidator>();
        services.AddScoped<IValidator<CategoriaDto>, CategoriaValidator>();
        services.AddScoped<IValidator<ComponenteDto>, ComponenteValidator>();
        services.AddScoped<IValidator<EquipoDto>, EquipoValidator>();
        services.AddScoped<IValidator<GaveteroDto>, GaveteroValidator>();
        services.AddScoped<IValidator<GrupoEquipoDto>, GrupoEquipoValidator>();
        services.AddScoped<IValidator<MuebleDto>, MuebleValidator>();

        if (ElasticsearchSettings.IsConfigured(configuration))
            services.AddHostedService<SearchReindexer>();
        return services;
    }
}
