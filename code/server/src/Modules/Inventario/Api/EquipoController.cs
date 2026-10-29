using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/equipos")]
public class EquipoController : ApiController
{
    private readonly EquipoService _service;

    public EquipoController(EquipoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? grupoId = null,
        [FromQuery] int? gaveteroId = null
    )
    {
        if (grupoId.HasValue)
            return ToResponse(await _service.GetByGrupo(grupoId.Value));

        if (gaveteroId.HasValue)
            return ToResponse(await _service.GetByGavetero(gaveteroId.Value));

        return ToResponse(await _service.GetAll());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EquipoDto dto)
    {
        var result = await _service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EquipoDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpGet("{id:int}/prestamos")]
    public async Task<IActionResult> GetPrestamos(int id) =>
        ToResponse(await _service.GetHistorial(id));
}
