using FluentValidation;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Mantenimientos.Application;
using UCB_Hold.Server.Modules.Mantenimientos.Domain;
using UCB_Hold.Server.Modules.Mantenimientos.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Mantenimientos;

public static class MantenimientosModule
{
    public static IServiceCollection AddMantenimientosModule(this IServiceCollection services)
    {
        services.AddScoped<IAuditEntityNames, MantenimientosAuditNames>();
        services.AddScoped<IEquipoEliminacion, MantenimientosDeEquipoEliminado>();
        services.AddScoped<MantenimientoRepository>();
        services.AddScoped<EmpresaMantenimientoRepository>();
        services.AddScoped<MantenimientoService>();
        services.AddScoped<Service<EmpresaMantenimiento, EmpresaMantenimientoRepository, EmpresaMantenimientoDto>>();
        services.AddScoped<EstadoMantenimientoService>();

        services.AddMapper<Mantenimiento, MantenimientoDto, MantenimientoMapper>();
        services.AddMapper<EmpresaMantenimiento, EmpresaMantenimientoDto, EmpresaMantenimientoMapper>();
        services.AddScoped<IValidator<MantenimientoDto>, MantenimientoValidator>();
        services.AddScoped<IValidator<EmpresaMantenimientoDto>, EmpresaMantenimientoValidator>();
        return services;
    }
}
