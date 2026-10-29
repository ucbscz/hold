using System.IdentityModel.Tokens.Jwt;
using System.Text;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace IMT_Reservas.Server.Presentation.Security;

public static class AuthenticationRegistration
{
    private static readonly TimeSpan ExternalSignInLifetime = TimeSpan.FromMinutes(10);

    public static IServiceCollection AddPlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        JwtSettings jwt
    )
    {
        var section = configuration.GetSection(AuthenticationSettings.SectionName);
        var settings = section.Get<AuthenticationSettings>() ?? new AuthenticationSettings();
        if (string.IsNullOrWhiteSpace(settings.InstitutionalDomain))
            throw new InvalidOperationException(
                $"{AuthenticationSettings.SectionName}:{nameof(AuthenticationSettings.InstitutionalDomain)} is required."
            );

        var providerName = settings.Provider.ToString();
        var credentials =
            section.GetSection(providerName).Get<ExternalProviderCredentials>()
            ?? new ExternalProviderCredentials();
        if (credentials.IsPartial)
            throw new InvalidOperationException(
                $"{AuthenticationSettings.SectionName}:{providerName} ClientId and ClientSecret must be configured together."
            );

        services.Configure<AuthenticationSettings>(section);
        services.AddSingleton<InstitutionalEmailPolicy>();

        var builder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => ConfigureJwt(options, jwt))
            .AddCookie(AuthSchemes.External, options =>
            {
                options.Cookie.Name = AuthSchemes.ExternalCookie;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.ExpireTimeSpan = ExternalSignInLifetime;
            });

        var scheme = settings.Provider switch
        {
            ExternalIdentityProvider.Google => GoogleDefaults.AuthenticationScheme,
            _ => throw new InvalidOperationException(
                $"{AuthenticationSettings.SectionName}:{nameof(AuthenticationSettings.Provider)} {providerName} is not supported."
            ),
        };
        if (credentials.IsConfigured)
            AddProvider(builder, settings.Provider, scheme, credentials);

        services.AddSingleton(new ExternalIdentityScheme(scheme, credentials.IsConfigured));
        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    private static void AddProvider(
        AuthenticationBuilder builder,
        ExternalIdentityProvider provider,
        string scheme,
        ExternalProviderCredentials credentials
    )
    {
        switch (provider)
        {
            case ExternalIdentityProvider.Google:
                builder.AddGoogle(scheme, options =>
                {
                    options.SignInScheme = AuthSchemes.External;
                    options.ClientId = credentials.ClientId;
                    options.ClientSecret = credentials.ClientSecret;
                    options.CallbackPath = ApiPaths.ExternalCallback(scheme);
                    options.SaveTokens = false;
                });
                break;
        }
    }

    private static void ConfigureJwt(JwtBearerOptions options, JwtSettings jwt)
    {
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[AuthCookieService.AccessTokenName];
                return Task.CompletedTask;
            },
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = JwtService.RoleClaim,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            ClockSkew = TimeSpan.Zero,
        };
    }
}
