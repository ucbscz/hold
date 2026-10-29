using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Server.Presentation.Security;

public sealed class AuthCookieService
{
    public const string AccessTokenName = "ucbhold_access";
    public const string RefreshTokenName = "ucbhold_refresh";

    private readonly JwtSettings _settings;
    private readonly bool _secure;

    public AuthCookieService(IOptions<JwtSettings> settings, IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _secure = !environment.IsDevelopment();
    }

    public void Write(IResponseCookies cookies, LoginDto session)
    {
        cookies.Append(
            AccessTokenName,
            session.AccessToken,
            BuildOptions(ApiPaths.Api, TimeSpan.FromMinutes(_settings.ExpiresInMinutes))
        );
        cookies.Append(
            RefreshTokenName,
            session.RefreshToken,
            BuildOptions(ApiPaths.Auth, TimeSpan.FromDays(_settings.RefreshTokenExpiryDays))
        );
    }

    public void Clear(IResponseCookies cookies)
    {
        cookies.Delete(AccessTokenName, BuildOptions(ApiPaths.Api, TimeSpan.Zero));
        cookies.Delete(RefreshTokenName, BuildOptions(ApiPaths.Auth, TimeSpan.Zero));
    }

    private CookieOptions BuildOptions(string path, TimeSpan maxAge) =>
        new()
        {
            HttpOnly = true,
            Secure = _secure,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Path = path,
            MaxAge = maxAge,
        };
}
