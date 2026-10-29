using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Accesorio;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;
using IMT_Reservas.Server.Application.Features.Carrera;
using IMT_Reservas.Server.Application.Features.Carrito;
using IMT_Reservas.Server.Application.Features.Categoria;
using IMT_Reservas.Server.Application.Features.Componente;
using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Application.Features.EmpresaMantenimiento;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Application.Features.Gavetero;
using IMT_Reservas.Server.Application.Features.GrupoEquipo;
using IMT_Reservas.Server.Application.Features.Mantenimiento;
using IMT_Reservas.Server.Application.Features.Mueble;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Application.Features.Inventario;
using IMT_Reservas.Server.Application.Security;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Email;
using IMT_Reservas.Server.Infrastructure.Jobs;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Server.Presentation.Errors;
using IMT_Reservas.Server.Presentation.Security;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Npgsql;
using IMT_Reservas.Server.Infrastructure.Config.RowLevelSecurity;
using CarreraEntity = IMT_Reservas.Server.Core.Entities.Carrera;
using CategoriaEntity = IMT_Reservas.Server.Core.Entities.Categoria;
using EmpresaMantenimientoEntity = IMT_Reservas.Server.Core.Entities.EmpresaMantenimiento;
using JwtSettings = IMT_Reservas.Server.Application.Features.Jwt.JwtSettings;
using JwtSvc = IMT_Reservas.Server.Application.Features.Jwt.JwtService;
using MuebleEntity = IMT_Reservas.Server.Core.Entities.Mueble;

const string FrontendCorsPolicy = "AllowFrontend";

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
ServerConfigurationValidator.Validate(builder.Configuration);

builder.Services.AddDatabase(builder.Configuration);

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var allowedOrigins = builder.Configuration.GetSection(ConfigurationKeys.AllowedOrigins).Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        FrontendCorsPolicy,
        corsBuilder =>
        {
            corsBuilder
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste your JWT access token here",
        }
    );
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});
builder.Services.AddHealthChecks();
DataProtectionConfiguration.Configure(builder.Services, builder.Configuration, builder.Environment);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(ConfigurationKeys.Jwt));
builder.Services.AddSingleton<JwtSvc>();
builder.Services.AddSingleton<AuthCookieService>();

var jwtSettings = builder.Configuration.GetSection(ConfigurationKeys.Jwt).Get<JwtSettings>()!;

builder.Services.AddPlatformAuthentication(builder.Configuration, jwtSettings);

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioAuthRepository>();
builder.Services.AddScoped<UsuarioReadRepository>();
builder.Services.AddScoped<CodigoAutenticacionRepository>();
builder.Services.AddScoped<PrestamoRepository>();
builder.Services.AddScoped<PrestamoReadRepository>();
builder.Services.AddScoped<PrestamoEstadoRepository>();
builder.Services.AddScoped<PrestamoDisponibilidadRepository>();
builder.Services.AddScoped<EquipoRepository>();
builder.Services.AddInventoryCatalogs();
builder.Services.AddScoped<AccesorioRepository>();
builder.Services.AddScoped<GrupoEquipoRepository>();
builder.Services.AddScoped<Repository<CarreraEntity, CarreraDto>>();
builder.Services.AddScoped<Repository<CategoriaEntity, CategoriaDto>>();
builder.Services.AddScoped<Repository<MuebleEntity, MuebleDto>>();
builder.Services.AddScoped<MantenimientoRepository>();
builder.Services.AddScoped<EmpresaMantenimientoRepository>();
builder.Services.AddScoped<GaveteroRepository>();
builder.Services.AddScoped<ContratoRepository>();
builder.Services.AddSingleton<ContractHtmlProcessor>();
builder.Services.AddScoped<ComponenteRepository>();
builder.Services.AddScoped<CarritoRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddEmailDelivery(builder.Configuration);
builder.Services.AddScoped<VerificacionCorreoService>();
builder.Services.AddScoped<RecuperacionContrasenaService>();
builder.Services.AddScoped<CodigoGoogleService>();
builder.Services.AddScoped<AutenticacionGoogleService>();
builder.Services.AddSingleton<SensitiveDataProtector>();
builder.Services.AddScoped<CarritoService>();
builder.Services.AddScoped<PrestamoService>();
builder.Services.AddScoped<EquipoService>();
builder.Services.AddScoped<GrupoEquipoService>();
builder.Services.AddSingleton<GrupoEquipoImportacionService>();
builder.Services.AddScoped<MantenimientoService>();
builder.Services.AddScoped<GaveteroService>();
builder.Services.AddScoped<ContratoService>();
builder.Services.AddScoped<ConfiguracionRepository>();
builder.Services.AddScoped<ConfiguracionService>();

builder.Services.AddScoped<
    Service<CarreraEntity, Repository<CarreraEntity, CarreraDto>, CarreraDto>
>();
builder.Services.AddScoped<
    Service<CategoriaEntity, Repository<CategoriaEntity, CategoriaDto>, CategoriaDto>
>();
builder.Services.AddScoped<
    Service<MuebleEntity, Repository<MuebleEntity, MuebleDto>, MuebleDto>
>();
builder.Services.AddScoped<
    Service<EmpresaMantenimientoEntity, EmpresaMantenimientoRepository, EmpresaMantenimientoDto>
>();
builder.Services.AddScoped<Service<Accesorio, AccesorioRepository, AccesorioDto>>();
builder.Services.AddScoped<Service<Componente, ComponenteRepository, ComponenteDto>>();

builder.Services.AddSingleton<UsuarioMapper>();
builder.Services.AddSingleton<PrestamoMapper>();
builder.Services.AddSingleton<EquipoMapper>();
builder.Services.AddSingleton<CarreraMapper>();
builder.Services.AddSingleton<CategoriaMapper>();
builder.Services.AddSingleton<GrupoEquipoMapper>();
builder.Services.AddSingleton<GaveteroMapper>();
builder.Services.AddSingleton<MuebleMapper>();
builder.Services.AddSingleton<MantenimientoMapper>();
builder.Services.AddSingleton<EmpresaMantenimientoMapper>();
builder.Services.AddSingleton<AccesorioMapper>();
builder.Services.AddSingleton<ComponenteMapper>();
builder.Services.AddSingleton<ContratoMapper>();

builder.Services.AddSingleton<IMapper<Usuario, UsuarioDto>>(sp =>
    sp.GetRequiredService<UsuarioMapper>()
);
builder.Services.AddSingleton<IMapper<Prestamo, PrestamoDto>>(sp =>
    sp.GetRequiredService<PrestamoMapper>()
);
builder.Services.AddSingleton<IMapper<Equipo, EquipoDto>>(sp =>
    sp.GetRequiredService<EquipoMapper>()
);
builder.Services.AddSingleton<IMapper<CarreraEntity, CarreraDto>>(sp =>
    sp.GetRequiredService<CarreraMapper>()
);
builder.Services.AddSingleton<IMapper<CategoriaEntity, CategoriaDto>>(sp =>
    sp.GetRequiredService<CategoriaMapper>()
);
builder.Services.AddSingleton<IMapper<GrupoEquipo, GrupoEquipoDto>>(sp =>
    sp.GetRequiredService<GrupoEquipoMapper>()
);
builder.Services.AddSingleton<IMapper<Gavetero, GaveteroDto>>(sp =>
    sp.GetRequiredService<GaveteroMapper>()
);
builder.Services.AddSingleton<IMapper<MuebleEntity, MuebleDto>>(sp =>
    sp.GetRequiredService<MuebleMapper>()
);
builder.Services.AddSingleton<IMapper<Mantenimiento, MantenimientoDto>>(sp =>
    sp.GetRequiredService<MantenimientoMapper>()
);
builder.Services.AddSingleton<IMapper<EmpresaMantenimientoEntity, EmpresaMantenimientoDto>>(sp =>
    sp.GetRequiredService<EmpresaMantenimientoMapper>()
);
builder.Services.AddSingleton<IMapper<Accesorio, AccesorioDto>>(sp =>
    sp.GetRequiredService<AccesorioMapper>()
);
builder.Services.AddSingleton<IMapper<Componente, ComponenteDto>>(sp =>
    sp.GetRequiredService<ComponenteMapper>()
);
builder.Services.AddSingleton<IMapper<Contrato, ContratoDto>>(sp =>
    sp.GetRequiredService<ContratoMapper>()
);

builder.Services.AddScoped<IValidator<AccesorioDto>, AccesorioValidator>();
builder.Services.AddScoped<IValidator<CarreraDto>, CarreraValidator>();
builder.Services.AddScoped<IValidator<ConfiguracionDto>, ConfiguracionValidator>();
builder.Services.AddScoped<ConfiguracionMapper>();
builder.Services.AddScoped<IMapper<ConfiguracionSistema, ConfiguracionDto>>(sp =>
    sp.GetRequiredService<ConfiguracionMapper>()
);
builder.Services.AddScoped<IValidator<CategoriaDto>, CategoriaValidator>();
builder.Services.AddScoped<IValidator<ComponenteDto>, ComponenteValidator>();
builder.Services.AddScoped<IValidator<ContratoDto>, ContratoValidator>();
builder.Services.AddScoped<IValidator<EmpresaMantenimientoDto>, EmpresaMantenimientoValidator>();
builder.Services.AddScoped<IValidator<EquipoDto>, EquipoValidator>();
builder.Services.AddScoped<IValidator<GaveteroDto>, GaveteroValidator>();
builder.Services.AddScoped<IValidator<GrupoEquipoDto>, GrupoEquipoValidator>();
builder.Services.AddScoped<IValidator<MantenimientoDto>, MantenimientoValidator>();
builder.Services.AddScoped<IValidator<MuebleDto>, MuebleValidator>();
builder.Services.AddScoped<IValidator<PrestamoDto>, PrestamoValidator>();
builder.Services.AddScoped<IValidator<UsuarioDto>, UsuarioValidator>();

builder.Services.AddDistributedCaching(builder.Configuration, builder.Environment);

builder.Services.AddSearch(builder.Configuration);

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogRepository>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<NotificacionRepository>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<AvisoDisponibilidadRepository>();
builder.Services.AddScoped<AvisoDisponibilidadService>();
builder.Services.AddScoped<ComentarioEquipoRepository>();
builder.Services.AddScoped<ComentarioEquipoService>();

builder.Services.AddBackgroundJobs(builder.Configuration, builder.Environment);

builder.Services.AddRateLimiter(RequestLimits.Configure);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
    await RowLevelSecurityGuard.EnsureEnforcedAsync(app.Services.GetRequiredService<NpgsqlDataSource>());
var useHttpsRedirection = builder.Configuration.GetValue("HttpsRedirection:Enabled", true);

app.UseForwardedHeaders();
app.UseCookiePolicy();

if (!app.Environment.IsDevelopment() && useHttpsRedirection)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSecurityHeaders();

app.UseCors(FrontendCorsPolicy);
app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks(ApiPaths.Health).AllowAnonymous();
app.UseBackgroundJobs();

await app.RunAsync();
