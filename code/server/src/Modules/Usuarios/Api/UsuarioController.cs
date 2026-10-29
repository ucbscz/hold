using IMT_Reservas.Server.Application.Features.Usuario;
using Ardalis.Result;
using IMT_Reservas.Server.Core.Entities;
using Microsoft.AspNetCore.RateLimiting;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Infrastructure.Config;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/usuarios")]
public class UsuarioController : Controller
{
    private readonly UsuarioService _service;

    public UsuarioController(UsuarioService service) => _service = service;

    [HttpGet("perfil")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken) =>
        ToResponse(
            await _service.GetProfile(
                CurrentCarnet,
                cancellationToken
            )
        );

    [HttpPut("perfil")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UsuarioDto dto,
        CancellationToken cancellationToken
    )
    {
        var carnet = CurrentCarnet;
        return ToResponse(
            await _service.UpdateProfile(
                carnet,
                dto,
                cancellationToken
            )
        );
    }

    [Authorize(Roles = Permisos.Gestion)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = UsuarioReadRepository.MaxPageSize,
        CancellationToken cancellationToken = default
    ) => ToResponse(await _service.GetAll(pagina, tamanoPagina, cancellationToken));

    [HttpGet("{carnet}")]
    public async Task<IActionResult> Get(string carnet, CancellationToken cancellationToken)
    {
        if (!User.PuedeGestionar() && CurrentCarnet != carnet)
            return Forbid();

        return ToResponse(await _service.Get(carnet, cancellationToken));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RequestLimits.AuthPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UsuarioDto dto,
        CancellationToken cancellationToken
    )
    {
        if (!User.PuedeGestionar() && string.IsNullOrWhiteSpace(dto.CodigoGoogle))
            return ToResponse(
                Result<UsuarioDto>.Unauthorized(
                    "Verifica tu correo institucional con Google para crear tu cuenta"
                )
            );

        var result = await _service.Create(
            dto,
            User.PuedeGestionar(),
            User.IsInRole(Permisos.AdministradorLaboratorio),
            cancellationToken
        );
        return ToCreatedResponse(result, nameof(Get), new { carnet = result.Value?.Carnet });
    }

    [HttpPut("{carnet}")]
    public async Task<IActionResult> Update(
        string carnet,
        [FromBody] UsuarioDto dto,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
            await _service.Update(
                carnet,
                dto,
                CurrentCarnet,
                User.PuedeGestionar(),
                User.IsInRole(Permisos.AdministradorLaboratorio),
                cancellationToken
            )
        );

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{carnet}")]
    public async Task<IActionResult> Delete(string carnet, CancellationToken cancellationToken) =>
        ToDeleteResponse(await _service.Delete(carnet, cancellationToken));

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
                CurrentCarnet,
                User.IsInRole(Permisos.AdministradorLaboratorio),
                cancellationToken
            )
        );

}
