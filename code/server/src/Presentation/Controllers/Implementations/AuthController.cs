using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Presentation.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.Net.Http.Headers;
using IMT_Reservas.Server.Application.Abstraction;
using Microsoft.Extensions.Options;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[AllowAnonymous]
[EnableRateLimiting(RequestLimits.AuthPolicy)]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly UsuarioService _service;
    private readonly VerificacionCorreoService _verification;
    private readonly RecuperacionContrasenaService _passwordRecovery;
    private readonly AutenticacionGoogleService _google;
    private readonly AuthenticationSettings _authentication;
    private readonly ExternalIdentityScheme _externalScheme;
    private readonly AuthCookieService _cookies;

    public AuthController(
        UsuarioService service,
        VerificacionCorreoService verification,
        RecuperacionContrasenaService passwordRecovery,
        AutenticacionGoogleService google,
        IOptions<AuthenticationSettings> authentication,
        ExternalIdentityScheme externalScheme,
        AuthCookieService cookies
    )
    {
        _service = service;
        _verification = verification;
        _passwordRecovery = passwordRecovery;
        _google = google;
        _authentication = authentication.Value;
        _externalScheme = externalScheme;
        _cookies = cookies;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _service.Login(request.Email, request.Contrasena, cancellationToken);
        return SessionResponse(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[AuthCookieService.RefreshTokenName] ?? string.Empty;
        var result = await _service.Refresh(refreshToken, cancellationToken);
        if (!result.IsSuccess)
            _cookies.Clear(Response.Cookies);
        return SessionResponse(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[AuthCookieService.RefreshTokenName] ?? string.Empty;
        await _service.Logout(refreshToken, cancellationToken);
        _cookies.Clear(Response.Cookies);
        return NoContent();
    }

    [HttpPost("verificar")]
    public async Task<IActionResult> Verify(
        [FromBody] TokenDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _verification.Confirm(request.Token, cancellationToken));

    [HttpPost("reenviar")]
    public async Task<IActionResult> Resend(
        [FromBody] EmailDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _verification.Resend(request.Email, cancellationToken));

    [HttpPost("recuperar")]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] EmailDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _passwordRecovery.Request(request.Email, cancellationToken);
        return result.Status == ResultStatus.Error
            ? StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { Status = 503, Errors = result.Errors.ToList() }
            )
            : ToResponse(result);
    }

    [HttpPost("restablecer")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request,
        CancellationToken cancellationToken
    ) => ToResponse(
        await _passwordRecovery.Reset(
            request.Email,
            request.Token,
            request.Contrasena,
            cancellationToken
        )
    );

    [HttpGet("google")]
    public IActionResult Google([FromQuery] string? origen)
    {
        var frontend = FrontendUrl;
        var ruta = GoogleRoute(origen);
        if (!_externalScheme.IsConfigured)
            return Redirect($"{frontend}/{ruta}?googleError=configuracion");

        return Challenge(
            new AuthenticationProperties { RedirectUri = $"/api/auth/google/resultado?origen={ruta}" },
            _externalScheme.Name
        );
    }

    [HttpGet("google/resultado")]
    public async Task<IActionResult> GoogleResult(
        [FromQuery] string? origen,
        CancellationToken cancellationToken
    )
    {
        var authentication = await HttpContext.AuthenticateAsync(AuthSchemes.External);
        var frontend = FrontendUrl;
        var ruta = GoogleRoute(origen);
        if (!authentication.Succeeded || authentication.Principal == null)
            return Redirect($"{frontend}/{ruta}?googleError=cancelado");

        var result = await _google.Begin(authentication.Principal, cancellationToken);
        await HttpContext.SignOutAsync(AuthSchemes.External);
        if (!result.IsSuccess)
            return Redirect($"{frontend}/{ruta}?googleError=cuenta");

        return Redirect($"{frontend}/login?codigo={Uri.EscapeDataString(result.Value)}");
    }

    [HttpPost("google/intercambiar")]
    public async Task<IActionResult> ExchangeGoogle(
        [FromBody] TokenDto request,
        CancellationToken cancellationToken
    )
    {
        Response.Headers.CacheControl = CacheControlHeaderValue.NoStoreString;
        var result = await _google.Exchange(request.Token, cancellationToken);
        if (!result.IsSuccess)
            return ToResponse(result);

        var value = result.Value;
        AuthSessionResponseDto? session = null;
        if (value.Sesion != null)
        {
            _cookies.Write(Response.Cookies, value.Sesion);
            session = new AuthSessionResponseDto { Usuario = value.Sesion.Usuario };
        }

        return ToResponse(
            Result<GoogleAuthResponseDto>.Success(
                new GoogleAuthResponseDto
                {
                    RequiereRegistro = value.RequiereRegistro,
                    CodigoRegistro = value.CodigoRegistro,
                    Email = value.Email,
                    Nombre = value.Nombre,
                    ApellidoPaterno = value.ApellidoPaterno,
                    ApellidoMaterno = value.ApellidoMaterno,
                    Sesion = session,
                }
            )
        );
    }

    private string FrontendUrl => _authentication.FrontendBaseUrl;

    private static string GoogleRoute(string? origen) =>
        origen == TipoCodigoGoogle.Registro ? TipoCodigoGoogle.Registro : TipoCodigoGoogle.Login;

    private IActionResult SessionResponse(Result<LoginDto> result)
    {
        Response.Headers.CacheControl = CacheControlHeaderValue.NoStoreString;
        if (!result.IsSuccess)
            return ToResponse(result);

        _cookies.Write(Response.Cookies, result.Value);
        return ToResponse(
            Result<AuthSessionResponseDto>.Success(
                new AuthSessionResponseDto { Usuario = result.Value.Usuario }
            )
        );
    }
}
