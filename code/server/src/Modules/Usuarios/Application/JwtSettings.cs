namespace IMT_Reservas.Server.Application.Features.Jwt;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
