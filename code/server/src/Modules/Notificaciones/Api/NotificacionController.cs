using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Notificaciones.Application;

namespace UCB_Hold.Server.Modules.Notificaciones.Api;

[Authorize]
[Route("api/notificaciones")]
public class NotificacionController : ApiController
{
    private readonly NotificacionService _service;

    public NotificacionController(NotificacionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToResponse(await _service.GetByCarnet(CurrentCarnet));

    [HttpPatch("{id:int}/lectura")]
    public async Task<IActionResult> MarkAsRead(int id) =>
        ToResponse(await _service.MarkAsRead(id, CurrentCarnet));

    [HttpPatch("lectura")]
    public async Task<IActionResult> MarkAllAsRead() =>
        ToResponse(await _service.MarkAllAsRead(CurrentCarnet));

}
