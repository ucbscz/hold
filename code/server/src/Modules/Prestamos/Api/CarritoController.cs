using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Modules.Prestamos.Application;

namespace IMT_Reservas.Server.Modules.Prestamos.Api;

[Authorize]
[Route("api/carrito")]
public class CarritoController : ApiController
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
