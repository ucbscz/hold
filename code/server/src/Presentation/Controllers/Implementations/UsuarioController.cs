using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Core.Entities;
using Microsoft.AspNetCore.RateLimiting;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/usuarios")]
public class UsuarioController : Controller
{
    private readonly UsuarioService _service;

    public UsuarioController(UsuarioService service) => _service = service;

    [Authorize(Roles = Permisos.Gestion)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = UsuarioReadRepository.MaxPageSize,
        CancellationToken cancellationToken = default
    ) => ToResponse(await _service.GetAll(pagina, tamanoPagina, cancellationToken));

    [HttpGet("{carnet}")]
    public async Task<IActionResult> Get(string carnet)
    {
        if (!User.PuedeGestionar() && User.Identity?.Name != carnet)
            return Forbid();

        return ToResponse(await _service.Get(carnet));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UsuarioDto dto)
    {
        var result = await _service.Create(dto, User.PuedeGestionar(), User.IsInRole("administrador_laboratorio"));
        return ToCreatedResponse(result, nameof(Get), new { carnet = result.Value?.Carnet });
    }

    [HttpPut("{carnet}")]
    public async Task<IActionResult> Update(string carnet, [FromBody] UsuarioDto dto) =>
        ToResponse(
            await _service.Update(carnet, dto, User.Identity?.Name, User.PuedeGestionar(), User.IsInRole("administrador_laboratorio"))
        );

    [Authorize(Roles = "administrador")]
    [HttpDelete("{carnet}")]
    public async Task<IActionResult> Delete(string carnet) =>
        ToDeleteResponse(await _service.Delete(carnet));

    [Authorize(Roles = Permisos.Gestion)]
    [HttpPatch("{carnet}/bloqueo")]
    public async Task<IActionResult> SetBlocked(
        string carnet,
        [FromBody] UsuarioDto request,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
            await _service.SetBlocked(
                carnet,
                request.Bloqueado ?? false,
                request.MotivoBloqueo,
                User.PuedeGestionar(),
                User.Identity?.Name,
                cancellationToken,
                User.IsInRole("administrador_laboratorio")
            )
        );

}
