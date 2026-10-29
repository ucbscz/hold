using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/gaveteros")]
public class GaveteroController : ApiController
{
    private readonly GaveteroService _service;

    public GaveteroController(GaveteroService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? muebleId = null) =>
        muebleId.HasValue
            ? ToResponse(await _service.GetByMueble(muebleId.Value))
            : ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GaveteroDto dto)
    {
        var result = await _service.Create(dto);

        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GaveteroDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));

}
