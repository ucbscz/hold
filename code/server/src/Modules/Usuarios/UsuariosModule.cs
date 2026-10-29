using FluentValidation;
using IMT_Reservas.Server.Modules.Usuarios.Api;
using IMT_Reservas.Server.Modules.Usuarios.Application;
using IMT_Reservas.Server.Modules.Usuarios.Domain;
using IMT_Reservas.Server.Modules.Usuarios.Infrastructure;
using IMT_Reservas.Server.Shared.Configuration;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Usuarios;

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
        services.AddScoped<CodigoAutenticacionRepository>();
        services.AddScoped<Repository<Carrera, CarreraDto>>();

        services.AddScoped<UsuarioService>();
        services.AddScoped<EmailDeliveryService>();
        services.AddScoped<VerificacionCorreoService>();
        services.AddScoped<RecuperacionContrasenaService>();
        services.AddScoped<CodigoGoogleService>();
        services.AddScoped<AutenticacionGoogleService>();
        services.AddScoped<Service<Carrera, Repository<Carrera, CarreraDto>, CarreraDto>>();

        services.AddMapper<Usuario, UsuarioDto, UsuarioMapper>();
        services.AddMapper<Carrera, CarreraDto, CarreraMapper>();
        services.AddScoped<IValidator<UsuarioDto>, UsuarioValidator>();
        services.AddScoped<IValidator<CarreraDto>, CarreraValidator>();
        return services;
    }
}
