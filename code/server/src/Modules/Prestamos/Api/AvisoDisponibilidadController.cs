using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Prestamos.Application;

namespace UCB_Hold.Server.Modules.Prestamos.Api;

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
