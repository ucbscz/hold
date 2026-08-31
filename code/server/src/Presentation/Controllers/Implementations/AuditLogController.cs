using Ardalis.Result;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Application.Features.AuditLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize(Roles = Permisos.Gestion)]
[Route("api/auditoria")]
public class AuditLogController : Controller
{
    private readonly AuditLogService _service;

    public AuditLogController(AuditLogService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? entidad,
        [FromQuery] string? actor,
        [FromQuery] string? accion,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta
    )
    {
        if (!User.IsInRole("administrador") && entidad is not ("Prestamo" or "Usuario"))
            return Forbid();
        return ToResponse(await _service.GetFiltered(entidad, actor, accion, desde, hasta));
    }
}
