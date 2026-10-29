using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmpresaMantenimientoEntity = UCB_Hold.Server.Modules.Mantenimientos.Domain.EmpresaMantenimiento;
using UCB_Hold.Server.Modules.Mantenimientos.Application;
using UCB_Hold.Server.Modules.Mantenimientos.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Mantenimientos.Api;

[Authorize(Roles = Permisos.Administrador)]
[Route("api/empresas")]
public class EmpresaMantenimientoController : ApiController
{
    private readonly Service<
        EmpresaMantenimientoEntity,
        EmpresaMantenimientoRepository,
        EmpresaMantenimientoDto
    > _service;

    public EmpresaMantenimientoController(
        Service<
            EmpresaMantenimientoEntity,
            EmpresaMantenimientoRepository,
            EmpresaMantenimientoDto
        > service
    ) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmpresaMantenimientoDto dto)
    {
        var result = await _service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmpresaMantenimientoDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
