using Ardalis.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Options;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Shared.Web;

namespace UCB_Hold.Server.Modules.Usuarios.Api;

[AllowAnonymous]
[EnableRateLimiting(RequestLimits.AuthPolicy)]
[Route("api/auth")]
public class AuthController : ApiController
{
    private readonly UsuarioService _service;
    private readonly VerificacionCorreoService _verification;
    private readonly RecuperacionContrasenaService _passwordRecovery;
    private readonly AutenticacionExternaService _external;
    private readonly AuthenticationSettings _authentication;
    private readonly ExternalIdentityScheme _externalScheme;
    private readonly AuthCookieService _cookies;

    public AuthController(
        UsuarioService service,
        VerificacionCorreoService verification,
        RecuperacionContrasenaService passwordRecovery,
        AutenticacionExternaService external,
        IOptions<AuthenticationSettings> authentication,
        ExternalIdentityScheme externalScheme,
        AuthCookieService cookies
    )
    {
        _service = service;
        _verification = verification;
        _passwordRecovery = passwordRecovery;
        _external = external;
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

    [HttpGet("externo")]
    public IActionResult External([FromQuery] string? origen)
    {
        var frontend = FrontendUrl;
        var ruta = ExternalRoute(origen);
        if (!_externalScheme.IsConfigured)
            return Redirect($"{frontend}/{ruta}?errorExterno=configuracion");

        return Challenge(
            new AuthenticationProperties { RedirectUri = $"/api/auth/externo/resultado?origen={ruta}" },
            _externalScheme.Name
        );
    }

    [HttpGet("externo/resultado")]
    public async Task<IActionResult> ExternalResult(
        [FromQuery] string? origen,
        CancellationToken cancellationToken
    )
    {
        var authentication = await HttpContext.AuthenticateAsync(AuthSchemes.External);
        var frontend = FrontendUrl;
        var ruta = ExternalRoute(origen);
        if (!authentication.Succeeded || authentication.Principal == null)
            return Redirect($"{frontend}/{ruta}?errorExterno=cancelado");

        var result = await _external.Begin(authentication.Principal, cancellationToken);
        await HttpContext.SignOutAsync(AuthSchemes.External);
        if (!result.IsSuccess)
            return Redirect($"{frontend}/{ruta}?errorExterno=cuenta");

        return Redirect($"{frontend}/login?codigo={Uri.EscapeDataString(result.Value)}");
    }

    [HttpPost("externo/intercambiar")]
    public async Task<IActionResult> ExchangeExternal(
        [FromBody] TokenDto request,
        CancellationToken cancellationToken
    )
    {
        Response.Headers.CacheControl = CacheControlHeaderValue.NoStoreString;
        var result = await _external.Exchange(request.Token, cancellationToken);
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
            Result<ExternalAuthResponseDto>.Success(
                new ExternalAuthResponseDto
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

    private static string ExternalRoute(string? origen) =>
        origen == TipoCodigoExterno.Registro ? TipoCodigoExterno.Registro : TipoCodigoExterno.Login;

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
