using IMT_Reservas.Server.Application.Features.Carrito;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/carrito")]
public class CarritoController : Controller
{
    private readonly CarritoService _service;

    public CarritoController(CarritoService service) => _service = service;

    [HttpPost("disponibilidad")]
    public async Task<IActionResult> DisponibilidadEquipos(
        [FromBody] CarritoDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _service.GetDisponibilidad(request, cancellationToken));

    [HttpPost("disponibilidad/calendario")]
    public async Task<IActionResult> DisponibilidadCalendario(
        [FromBody] DisponibilidadCalendarioDto request,
        CancellationToken cancellationToken
    ) => ToResponse(await _service.GetDisponibilidadCalendario(request, cancellationToken));
}
