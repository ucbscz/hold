using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Modules.Prestamos.Application;

namespace IMT_Reservas.Server.Modules.Prestamos.Api;

[Authorize]
[Route("api/avisos")]
public class AvisoDisponibilidadController : ApiController
{
    private readonly AvisoDisponibilidadService _service;

    public AvisoDisponibilidadController(AvisoDisponibilidadService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AvisoDisponibilidadDto dto) =>
        ToResponse(await _service.Create(CurrentCarnet, dto));
}
