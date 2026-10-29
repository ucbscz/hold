using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Auditoria.Api;

[Authorize(Roles = Permisos.Gestion)]
[Route("api/auditoria")]
public class AuditLogController : ApiController
{
    private readonly AuditLogService _service;

    public AuditLogController(AuditLogService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? entidad,
        [FromQuery] string? actor,
        [FromQuery] string? accion,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? buscar
    )
    {
        return ToResponse(
            await _service.GetFiltered(
                User.IsInRole(Permisos.Administrador),
                entidad,
                actor,
                accion,
                desde,
                hasta,
                buscar
            )
        );
    }
}
