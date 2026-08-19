using IMT_Reservas.Server.Application.Features.Gavetero;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/gaveteros")]
public class GaveteroController : Controller
{
    private readonly GaveteroService _service;

    public GaveteroController(GaveteroService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = "administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GaveteroDto dto)
    {
        var result = await _service.Create(dto);

        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = "administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GaveteroDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = "administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));

    [Authorize(Roles = "administrador")]
    [HttpGet("por-mueble/{muebleId:int}")]
    public async Task<IActionResult> GetByMueble(int muebleId) =>
        ToResponse(await _service.GetByMueble(muebleId));
}
