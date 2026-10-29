using FluentValidation;
using IMT_Reservas.Server.Modules.Mantenimientos.Application;
using IMT_Reservas.Server.Modules.Mantenimientos.Domain;
using IMT_Reservas.Server.Modules.Mantenimientos.Infrastructure;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Mantenimientos;

public static class MantenimientosModule
{
    public static IServiceCollection AddMantenimientosModule(this IServiceCollection services)
    {
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
