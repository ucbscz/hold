using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Npgsql;
using UCB_Hold.Server.Bootstrap;
using UCB_Hold.Server.Modules.Auditoria;
using UCB_Hold.Server.Modules.Configuracion;
using UCB_Hold.Server.Modules.Inventario;
using UCB_Hold.Server.Modules.Mantenimientos;
using UCB_Hold.Server.Modules.Notificaciones;
using UCB_Hold.Server.Modules.Prestamos;
using UCB_Hold.Server.Modules.Usuarios;
using UCB_Hold.Server.Shared.Caching;
using UCB_Hold.Server.Shared.Configuration;
using UCB_Hold.Server.Shared.Email;
using UCB_Hold.Server.Shared.Persistence.RowLevelSecurity;
using UCB_Hold.Server.Shared.Search;
using UCB_Hold.Server.Shared.Security;
using UCB_Hold.Server.Shared.Web;

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
builder.Services.AddBranding(builder.Configuration);
builder.Services.AddRegion(builder.Configuration);
DataProtectionConfiguration.Configure(builder.Services, builder.Configuration, builder.Environment);

builder.Services.AddSingleton<SensitiveDataProtector>();
builder.Services.AddAuditoriaModule();
builder.Services.AddNotificacionesModule();
builder.Services.AddConfiguracionModule();
builder.Services.AddUsuariosModule(builder.Configuration);
builder.Services.AddInventarioModule(builder.Configuration);
builder.Services.AddPrestamosModule();
builder.Services.AddMantenimientosModule();
builder.Services.AddEmailDelivery(builder.Configuration);

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
