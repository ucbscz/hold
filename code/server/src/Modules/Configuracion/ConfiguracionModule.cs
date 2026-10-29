using FluentValidation;
using IMT_Reservas.Server.Modules.Configuracion.Application;
using IMT_Reservas.Server.Modules.Configuracion.Domain;
using IMT_Reservas.Server.Modules.Configuracion.Infrastructure;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Configuracion;

public static class ConfiguracionModule
{
    public static IServiceCollection AddConfiguracionModule(this IServiceCollection services)
    {
        services.AddScoped<ConfiguracionRepository>();
        services.AddScoped<ConfiguracionService>();
        services.AddScoped<ConfiguracionMapper>();
        services.AddScoped<IMapper<ConfiguracionSistema, ConfiguracionDto>>(sp =>
            sp.GetRequiredService<ConfiguracionMapper>()
        );
        services.AddScoped<IValidator<ConfiguracionDto>, ConfiguracionValidator>();
        return services;
    }
}
