using FluentValidation;
using IMT_Reservas.Server.Modules.Prestamos.Application;
using IMT_Reservas.Server.Modules.Prestamos.Domain;
using IMT_Reservas.Server.Modules.Prestamos.Infrastructure;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Prestamos;

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
        services.AddScoped<CarritoService>();
        services.AddScoped<PrestamoService>();
        services.AddScoped<ContratoService>();
        services.AddScoped<AvisoDisponibilidadService>();

        services.AddMapper<Prestamo, PrestamoDto, PrestamoMapper>();
        services.AddMapper<Contrato, ContratoDto, ContratoMapper>();
        services.AddScoped<IValidator<PrestamoDto>, PrestamoValidator>();
        services.AddScoped<IValidator<ContratoDto>, ContratoValidator>();
        return services;
    }
}
