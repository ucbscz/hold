namespace IMT_Reservas.Server.Infrastructure.Config;

public static class ConfigurationKeys
{
    public const string PostgreSql = "PostgreSQL";
    public const string Jwt = "Jwt";
    public const string Email = "Email";
    public const string AllowedOrigins = "AllowedOrigins";
    public const string GoogleClientId = "Authentication:Google:ClientId";
    public const string GoogleClientSecret = "Authentication:Google:ClientSecret";
    public const string FrontendUrl = "Authentication:FrontendUrl";
    private const string DefaultFrontendUrl = "http://localhost:4200";

    public static string GetFrontendUrl(IConfiguration configuration) =>
        configuration[FrontendUrl]?.TrimEnd('/') ?? DefaultFrontendUrl;
}
