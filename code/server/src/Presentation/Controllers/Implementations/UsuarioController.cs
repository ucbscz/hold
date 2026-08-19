using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/usuario")]
public class UsuarioController : Controller
{
    private readonly UsuarioService _service;

    public UsuarioController(UsuarioService service) => _service = service;

    [Authorize(Roles = "administrador")]
    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{carnet}")]
    public async Task<IActionResult> Get(string carnet) => ToResponse(await _service.Get(carnet));

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UsuarioDto dto)
    {
        var result = await _service.Create(dto, User.IsInRole("administrador"));
        return ToCreatedResponse(result, nameof(Get), new { carnet = result.Value?.Carnet });
    }

    [HttpPut("{carnet}")]
    public async Task<IActionResult> Update(string carnet, [FromBody] UsuarioDto dto) =>
        ToResponse(
            await _service.Update(carnet, dto, User.Identity?.Name, User.IsInRole("administrador"))
        );

    [Authorize(Roles = "administrador")]
    [HttpDelete("{carnet}")]
    public async Task<IActionResult> Delete(string carnet) =>
        ToDeleteResponse(await _service.Delete(carnet));

    [Authorize(Roles = "administrador")]
    [HttpPut("{carnet}/bloqueo")]
    public async Task<IActionResult> SetBlocked(string carnet, [FromBody] UsuarioDto request) =>
        ToResponse(
            await _service.SetBlocked(
                carnet,
                request.Bloqueado ?? false,
                request.MotivoBloqueo,
                User.IsInRole("administrador")
            )
        );

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UsuarioDto request) =>
        ToResponse(
            await _service.Login(request.Email ?? string.Empty, request.Contrasena ?? string.Empty)
        );

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto request) =>
        ToResponse(await _service.Refresh(request.RefreshToken));
}
