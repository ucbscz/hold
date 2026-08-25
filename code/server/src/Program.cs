using System.Text;
using System.Threading.RateLimiting;
using Elastic.Clients.Elasticsearch;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
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
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Jobs;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Server.Presentation.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using StackExchange.Redis;
using CarreraEntity = IMT_Reservas.Server.Core.Entities.Carrera;
using CategoriaEntity = IMT_Reservas.Server.Core.Entities.Categoria;
using EmpresaMantenimientoEntity = IMT_Reservas.Server.Core.Entities.EmpresaMantenimiento;
using JwtSettings = IMT_Reservas.Server.Application.Features.Jwt.JwtSettings;
using JwtSvc = IMT_Reservas.Server.Application.Features.Jwt.JwtService;
using MuebleEntity = IMT_Reservas.Server.Core.Entities.Mueble;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
ServerConfigurationValidator.Validate(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<EstadoPrestamo>("estado_prestamo");
dataSourceBuilder.MapEnum<TipoUsuario>("tipo_usuario");
dataSourceBuilder.MapEnum<EstadoEquipo>("estado_equipo");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(dataSource));

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
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

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtSvc>();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            RoleClaimType = "role",
            NameClaimType = "sub",
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<PrestamoRepository>();
builder.Services.AddScoped<EquipoRepository>();
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
builder.Services.AddScoped<CarritoService>();
builder.Services.AddScoped<PrestamoService>();
builder.Services.AddScoped<EquipoService>();
builder.Services.AddScoped<GrupoEquipoService>();
builder.Services.AddScoped<MantenimientoService>();
builder.Services.AddScoped<GaveteroService>();
builder.Services.AddScoped<ContratoService>();
builder.Services.AddScoped<ConfiguracionRepository>();
builder.Services.AddScoped<ConfiguracionService>();

builder.Services.AddScoped<
    Service<CarreraEntity, Repository<CarreraEntity, CarreraDto>, CarreraDto>
>(sp =>
    new Service<CarreraEntity, Repository<CarreraEntity, CarreraDto>, CarreraDto>(
        sp.GetRequiredService<Repository<CarreraEntity, CarreraDto>>(),
        sp.GetRequiredService<IValidator<CarreraDto>>(),
        sp.GetRequiredService<IMapper<CarreraEntity, CarreraDto>>(),
        sp.GetRequiredService<AuditLogService>()
    )
);
builder.Services.AddScoped<
    Service<CategoriaEntity, Repository<CategoriaEntity, CategoriaDto>, CategoriaDto>
>(sp =>
    new Service<CategoriaEntity, Repository<CategoriaEntity, CategoriaDto>, CategoriaDto>(
        sp.GetRequiredService<Repository<CategoriaEntity, CategoriaDto>>(),
        sp.GetRequiredService<IValidator<CategoriaDto>>(),
        sp.GetRequiredService<IMapper<CategoriaEntity, CategoriaDto>>(),
        sp.GetRequiredService<AuditLogService>()
    )
);
builder.Services.AddScoped<
    Service<MuebleEntity, Repository<MuebleEntity, MuebleDto>, MuebleDto>
>(sp =>
    new Service<MuebleEntity, Repository<MuebleEntity, MuebleDto>, MuebleDto>(
        sp.GetRequiredService<Repository<MuebleEntity, MuebleDto>>(),
        sp.GetRequiredService<IValidator<MuebleDto>>(),
        sp.GetRequiredService<IMapper<MuebleEntity, MuebleDto>>(),
        sp.GetRequiredService<AuditLogService>()
    )
);
builder.Services.AddScoped<
    Service<EmpresaMantenimientoEntity, EmpresaMantenimientoRepository, EmpresaMantenimientoDto>
>(sp =>
    new Service<
        EmpresaMantenimientoEntity,
        EmpresaMantenimientoRepository,
        EmpresaMantenimientoDto
    >(
        sp.GetRequiredService<EmpresaMantenimientoRepository>(),
        sp.GetRequiredService<IValidator<EmpresaMantenimientoDto>>(),
        sp.GetRequiredService<IMapper<EmpresaMantenimientoEntity, EmpresaMantenimientoDto>>(),
        sp.GetRequiredService<AuditLogService>()
    )
);
builder.Services.AddScoped<Service<Accesorio, AccesorioRepository, AccesorioDto>>(sp =>
    new Service<Accesorio, AccesorioRepository, AccesorioDto>(
        sp.GetRequiredService<AccesorioRepository>(),
        sp.GetRequiredService<IValidator<AccesorioDto>>(),
        sp.GetRequiredService<IMapper<Accesorio, AccesorioDto>>(),
        sp.GetRequiredService<AuditLogService>()
    )
);
builder.Services.AddScoped<Service<Componente, ComponenteRepository, ComponenteDto>>(sp =>
    new Service<Componente, ComponenteRepository, ComponenteDto>(
        sp.GetRequiredService<ComponenteRepository>(),
        sp.GetRequiredService<IValidator<ComponenteDto>>(),
        sp.GetRequiredService<IMapper<Componente, ComponenteDto>>(),
        sp.GetRequiredService<AuditLogService>()
    )
);

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
builder.Services.AddScoped<IMapper<ConfiguracionSistema, ConfiguracionDto>, ConfiguracionMapper>();
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

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
    redisConfig.ConnectTimeout = 5000;
    redisConfig.SyncTimeout = 1000;
    redisConfig.AbortOnConnectFail = false;
    redisConfig.ConnectRetry = 3;
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.ConfigurationOptions = redisConfig;
        options.InstanceName = builder.Configuration["Redis:InstanceName"];
    });
}
else
    builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<CacheRepository>();

var elasticUri = builder.Configuration["Elasticsearch:Uri"];
if (!string.IsNullOrWhiteSpace(elasticUri))
{
    var elasticSettings = new ElasticsearchClientSettings(
        new Uri(elasticUri)
    ).DefaultFieldNameInferrer(name => name);
    builder.Services.AddSingleton(new ElasticsearchClient(elasticSettings));
    builder.Services.AddSingleton(typeof(ISearchIndex<>), typeof(ElasticSearchIndex<>));
    builder.Services.AddHostedService<SearchReindexer>();
}
else
    builder.Services.AddSingleton(typeof(ISearchIndex<>), typeof(NullSearchIndex<>));

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
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

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString))
);
builder.Services.AddHangfireServer();

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync("Demasiadas solicitudes.", ct);
    };

    options.AddFixedWindowLimiter(
        "auth",
        o =>
        {
            o.PermitLimit = 10;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 0;
        }
    );

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }
        )
    );
});

var app = builder.Build();
var useHttpsRedirection = builder.Configuration.GetValue("HttpsRedirection:Enabled", true);

app.UseCookiePolicy();

if (!app.Environment.IsDevelopment() && useHttpsRedirection)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(
    async (HttpContext ctx, Func<Task> next) =>
    {
        ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        ctx.Response.Headers.Append("X-Frame-Options", "DENY");
        ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        ctx.Response.Headers.Append(
            "Permissions-Policy",
            "camera=(), microphone=(), geolocation=()"
        );
        ctx.Response.Headers.Append("X-XSS-Protection", "0");

        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self';"
            );
        }
        else
        {
#pragma warning disable S7039
            ctx.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self';"
            );
#pragma warning restore S7039
        }
        await next();
    }
);

app.UseRateLimiter();
app.UseCors("AllowFrontend");
app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");
app.UseHangfireDashboard(
    "/hangfire",
    new DashboardOptions { Authorization = [new HangfireDashboardAuthorizationFilter()] }
);
RecurringJob.AddOrUpdate<EstadoPrestamoJob>(
    "estado-prestamo",
    job => job.Execute(),
    "*/10 * * * *"
);

await app.RunAsync();
