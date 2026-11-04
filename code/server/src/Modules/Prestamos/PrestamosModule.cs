using FluentValidation;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Prestamos;

public static class PrestamosModule
{
    public static IServiceCollection AddPrestamosModule(this IServiceCollection services)
    {
        services.AddScoped<PrestamoRepository>();
        services.AddScoped<PrestamoReadRepository>();
        services.AddScoped<PrestamoEstadoRepository>();
        services.AddScoped<PrestamoDisponibilidadRepository>();
        services.AddScoped<ContratoRepository>();
        services.AddScoped<CarritoRepository>();
        services.AddScoped<AvisoDisponibilidadRepository>();

        services.AddSingleton<ContractHtmlProcessor>();
        services.AddSingleton<HorarioReserva>();
        services.AddScoped<CarritoService>();
        services.AddScoped<PrestamoService>();
        services.AddScoped<PrestamoConsultaService>();
        services.AddScoped<CambioEstadoPrestamoService>();
        services.AddScoped<RetornoEquiposService>();
        services.AddScoped<NotificacionPrestamoService>();
        services.AddScoped<ContratoService>();
        services.AddScoped<AvisoDisponibilidadService>();
        services.AddScoped<EstadoPrestamoService>();

        services.AddMapper<Prestamo, PrestamoDto, PrestamoMapper>();
        services.AddMapper<Contrato, ContratoDto, ContratoMapper>();
        services.AddScoped<IValidator<PrestamoDto>, PrestamoValidator>();
        services.AddScoped<IValidator<ContratoDto>, ContratoValidator>();
        return services;
    }
}
