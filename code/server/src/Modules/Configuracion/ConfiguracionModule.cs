using FluentValidation;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Configuracion.Application;
using UCB_Hold.Server.Modules.Configuracion.Domain;
using UCB_Hold.Server.Modules.Configuracion.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Configuracion;

public static class ConfiguracionModule
{
    public static IServiceCollection AddConfiguracionModule(this IServiceCollection services)
    {
        services.AddSingleton<IAuditEntityNames, ConfiguracionAuditNames>();
        services.AddScoped<ConfiguracionRepository>();
        services.AddScoped<IConfiguracionProvider>(sp =>
            sp.GetRequiredService<ConfiguracionRepository>()
        );
        services.AddScoped<ConfiguracionService>();
        services.AddScoped<ConfiguracionMapper>();
        services.AddScoped<IMapper<ConfiguracionSistema, ConfiguracionDto>>(sp =>
            sp.GetRequiredService<ConfiguracionMapper>()
        );
        services.AddScoped<IValidator<ConfiguracionDto>, ConfiguracionValidator>();
        return services;
    }
}
