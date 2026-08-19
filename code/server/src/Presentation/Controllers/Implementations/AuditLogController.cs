using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.AuditLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize(Roles = "administrador")]
[Route("api/audit-log")]
public class AuditLogController : Controller
{
    private readonly AuditLogService _service;

    public AuditLogController(AuditLogService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? entidad,
        [FromQuery] string? carnet,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta
    ) => ToResponse(await _service.GetFiltered(entidad, carnet, desde, hasta));
}
