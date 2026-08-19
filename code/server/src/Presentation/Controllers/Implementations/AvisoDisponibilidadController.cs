using IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/aviso-disponibilidad")]
public class AvisoDisponibilidadController : Controller
{
    private readonly AvisoDisponibilidadService _service;

    public AvisoDisponibilidadController(AvisoDisponibilidadService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AvisoDisponibilidadDto dto) =>
        ToResponse(await _service.Create(User.Identity?.Name ?? string.Empty, dto));
}
