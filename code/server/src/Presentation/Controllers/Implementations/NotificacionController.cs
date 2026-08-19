using IMT_Reservas.Server.Application.Features.Notificacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/notificaciones")]
public class NotificacionController : Controller
{
    private readonly NotificacionService _service;

    public NotificacionController(NotificacionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToResponse(await _service.GetByCarnet(CurrentCarnet));

    [HttpPut("{id:int}/leido")]
    public async Task<IActionResult> MarkAsRead(int id) =>
        ToResponse(await _service.MarkAsRead(id, CurrentCarnet));

    [HttpPut("leidos")]
    public async Task<IActionResult> MarkAllAsRead() =>
        ToResponse(await _service.MarkAllAsRead(CurrentCarnet));

    private string CurrentCarnet => User.Identity?.Name ?? string.Empty;
}
