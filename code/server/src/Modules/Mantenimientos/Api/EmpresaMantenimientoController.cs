using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmpresaMantenimientoEntity = IMT_Reservas.Server.Modules.Mantenimientos.Domain.EmpresaMantenimiento;
using IMT_Reservas.Server.Modules.Mantenimientos.Application;
using IMT_Reservas.Server.Modules.Mantenimientos.Infrastructure;
using IMT_Reservas.Server.Shared.Kernel;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Api;

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
