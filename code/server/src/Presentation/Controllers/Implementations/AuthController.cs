using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.AspNetCore.Authorization;
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

    public AuthController(UsuarioService service) => _service = service;

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] UsuarioDto request,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
            await _service.Login(
                request.Email ?? string.Empty,
                request.Contrasena ?? string.Empty,
                cancellationToken
            )
        );

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _service.Refresh(request.RefreshToken, cancellationToken));
}
