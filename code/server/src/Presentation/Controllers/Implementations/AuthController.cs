using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[AllowAnonymous]
[EnableRateLimiting("auth")]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly UsuarioService _service;
    private readonly VerificacionCorreoService _verification;
    private readonly RecuperacionContrasenaService _passwordRecovery;
    private readonly AutenticacionGoogleService _google;
    private readonly IConfiguration _configuration;

    public AuthController(
        UsuarioService service,
        VerificacionCorreoService verification,
        RecuperacionContrasenaService passwordRecovery,
        AutenticacionGoogleService google,
        IConfiguration configuration
    )
    {
        _service = service;
        _verification = verification;
        _passwordRecovery = passwordRecovery;
        _google = google;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
            await _service.Login(
                request.Email,
                request.Contrasena,
                cancellationToken
            )
        );

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _service.Refresh(request.RefreshToken, cancellationToken));

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
    ) => ToResponse(await _passwordRecovery.Request(request.Email, cancellationToken));

    [HttpPost("restablecer")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _passwordRecovery.Reset(request.Token, request.Contrasena, cancellationToken));

    [HttpGet("google")]
    public IActionResult Google([FromQuery] string? origen)
    {
        var frontend = _configuration["Authentication:FrontendUrl"]?.TrimEnd('/')
            ?? "http://localhost:4200";
        var ruta = origen == "registro" ? "registro" : "login";
        if (string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]))
            return Redirect($"{frontend}/{ruta}?googleError=configuracion");

        return Challenge(
            new AuthenticationProperties { RedirectUri = $"/api/auth/google/resultado?origen={ruta}" },
            "Google"
        );
    }

    [HttpGet("google/resultado")]
    public async Task<IActionResult> GoogleResult(
        [FromQuery] string? origen,
        CancellationToken cancellationToken
    )
    {
        var authentication = await HttpContext.AuthenticateAsync("GoogleExternal");
        var frontend = _configuration["Authentication:FrontendUrl"]?.TrimEnd('/')
            ?? "http://localhost:4200";
        var ruta = origen == "registro" ? "registro" : "login";
        if (!authentication.Succeeded || authentication.Principal == null)
            return Redirect($"{frontend}/{ruta}?googleError=cancelado");

        var result = await _google.Begin(authentication.Principal, cancellationToken);
        await HttpContext.SignOutAsync("GoogleExternal");
        if (!result.IsSuccess)
            return Redirect($"{frontend}/{ruta}?googleError=cuenta");

        return Redirect($"{frontend}/login?codigo={Uri.EscapeDataString(result.Value)}");
    }

    [HttpPost("google/intercambiar")]
    public async Task<IActionResult> ExchangeGoogle(
        [FromBody] TokenDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _google.Exchange(request.Token, cancellationToken));
}
