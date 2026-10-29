using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Modules.Usuarios.Infrastructure;
using UCB_Hold.Server.Shared.Security;
using UCB_Hold.Server.Shared.Web;

namespace UCB_Hold.Server.Modules.Usuarios.Api;

[Authorize]
[Route("api/usuarios")]
public class UsuarioController : ApiController
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
        return ToResponse(
            await _service.GetVisible(carnet, CurrentCarnet, User.PuedeGestionar(), cancellationToken)
        );
    }

    [AllowAnonymous]
    [EnableRateLimiting(RequestLimits.AuthPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UsuarioDto dto,
        CancellationToken cancellationToken
    )
    {
        var result = await _service.Registrar(
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
