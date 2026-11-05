using FluentValidation;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Notificaciones.Application;
using UCB_Hold.Server.Modules.Usuarios.Api;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Modules.Usuarios.Infrastructure;
using UCB_Hold.Server.Shared.Configuration;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Usuarios;

public static class UsuariosModule
{
    public static IServiceCollection AddUsuariosModule(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtSection = configuration.GetSection(ConfigurationKeys.Jwt);
        services.Configure<JwtSettings>(jwtSection);
        services.AddSingleton<JwtService>();
        services.AddSingleton<AuthCookieService>();
        services.AddPlatformAuthentication(configuration, jwtSection.Get<JwtSettings>()!);

        services.AddScoped<UsuarioRepository>();
        services.AddScoped<UsuarioAuthRepository>();
        services.AddScoped<UsuarioReadRepository>();
        services.AddScoped<IUsuarioBloqueo>(sp => sp.GetRequiredService<UsuarioRepository>());
        services.AddScoped<IUsuarioConsultas>(sp => sp.GetRequiredService<UsuarioReadRepository>());
        services.AddScoped<IAuditActorDirectory>(sp => sp.GetRequiredService<UsuarioReadRepository>());
        services.AddScoped<IAuditEntityNames, UsuariosAuditNames>();
        services.AddScoped<IDestinatariosAdministradores>(sp => sp.GetRequiredService<UsuarioReadRepository>());
        services.AddScoped<CodigoAutenticacionRepository>();
        services.AddScoped<Repository<Carrera, CarreraDto>>();

        services.AddScoped<UsuarioService>();
        services.AddScoped<SesionService>();
        services.AddScoped<BloqueoUsuarioService>();
        services.AddScoped<EmailDeliveryService>();
        services.AddScoped<VerificacionCorreoService>();
        services.AddScoped<RecuperacionContrasenaService>();
        services.AddScoped<CodigoExternoService>();
        services.AddScoped<AutenticacionExternaService>();
        services.AddScoped<Service<Carrera, Repository<Carrera, CarreraDto>, CarreraDto>>();

        services.AddMapper<Usuario, UsuarioDto, UsuarioMapper>();
        services.AddMapper<Carrera, CarreraDto, CarreraMapper>();
        services.AddScoped<IValidator<UsuarioDto>, UsuarioValidator>();
        services.AddScoped<IValidator<CarreraDto>, CarreraValidator>();
        return services;
    }
}
