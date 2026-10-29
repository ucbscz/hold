using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Modules.Auditoria.Application;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Auditoria.Api;

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
