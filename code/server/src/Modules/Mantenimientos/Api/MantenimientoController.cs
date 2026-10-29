using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Mantenimientos.Application;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Mantenimientos.Api;

[Authorize(Roles = Permisos.Administrador)]
[Route("api/mantenimientos")]
public class MantenimientoController : ApiController
{
    private readonly MantenimientoService _service;

    public MantenimientoController(MantenimientoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MantenimientoDto dto)
    {
        var result = await _service.Create(dto);

        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MantenimientoDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
