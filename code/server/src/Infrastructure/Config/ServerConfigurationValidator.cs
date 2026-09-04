using IMT_Reservas.Server.Application.Features.Jwt;

namespace IMT_Reservas.Server.Infrastructure.Config;

public static class ServerConfigurationValidator
{
    private const int MinimumJwtKeyLength = 32;

    public static void Validate(IConfiguration configuration)
    {
        ValidateConnectionString(configuration.GetConnectionString("PostgreSQL"));
        ValidateJwtSettings(configuration.GetSection("Jwt").Get<JwtSettings>());
        ValidateAuthentication(configuration);
    }

    private static void ValidateAuthentication(IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) != string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException(
                "Authentication:Google:ClientId and ClientSecret must be configured together."
            );

        if (!configuration.GetValue<bool>("Email:Enabled"))
            return;

        if (string.IsNullOrWhiteSpace(configuration["Email:Host"]))
            throw new InvalidOperationException("Email:Host is required when email is enabled.");
        if (string.IsNullOrWhiteSpace(configuration["Email:From"]))
            throw new InvalidOperationException("Email:From is required when email is enabled.");
        if (configuration.GetValue<int>("Email:Port") is < 1 or > 65535)
            throw new InvalidOperationException("Email:Port must be between 1 and 65535.");
    }

    private static void ValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:PostgreSQL is required.");
    }

    private static void ValidateJwtSettings(JwtSettings? jwtSettings)
    {
        if (jwtSettings is null)
            throw new InvalidOperationException("Jwt settings are required.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
            throw new InvalidOperationException("Jwt:Key is required.");

        if (jwtSettings.Key.Length < MinimumJwtKeyLength)
            throw new InvalidOperationException(
                $"Jwt:Key must contain at least {MinimumJwtKeyLength} characters."
            );

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
            throw new InvalidOperationException("Jwt:Issuer is required.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
            throw new InvalidOperationException("Jwt:Audience is required.");

        if (jwtSettings.ExpiresInMinutes <= 0)
            throw new InvalidOperationException("Jwt:ExpiresInMinutes must be greater than zero.");

        if (jwtSettings.RefreshTokenExpiryDays <= 0)
            throw new InvalidOperationException(
                "Jwt:RefreshTokenExpiryDays must be greater than zero."
            );
    }
}
