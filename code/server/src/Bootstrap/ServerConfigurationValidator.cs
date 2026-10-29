using IMT_Reservas.Server.Application.Features.Jwt;

namespace IMT_Reservas.Server.Infrastructure.Config;

public static class ServerConfigurationValidator
{
    private const int MinimumJwtKeyLength = 32;

    public static void Validate(IConfiguration configuration)
    {
        ValidateConnectionString(configuration.GetConnectionString(ConfigurationKeys.PostgreSql));
        ValidateJwtSettings(configuration.GetSection(ConfigurationKeys.Jwt).Get<JwtSettings>());
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
