using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ComponenteEntity = IMT_Reservas.Server.Modules.Inventario.Domain.Componente;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Modules.Inventario.Infrastructure;
using IMT_Reservas.Server.Shared.Kernel;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/componentes")]
public class ComponenteController : ApiController
{
    private readonly Service<ComponenteEntity, ComponenteRepository, ComponenteDto> _service;

    public ComponenteController(
        Service<ComponenteEntity, ComponenteRepository, ComponenteDto> service
    ) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ComponenteDto dto)
    {
        var result = await _service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ComponenteDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
