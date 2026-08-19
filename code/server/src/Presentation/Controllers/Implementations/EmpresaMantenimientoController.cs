using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.EmpresaMantenimiento;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;
using EmpresaMantenimientoEntity = IMT_Reservas.Server.Core.Entities.EmpresaMantenimiento;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize(Roles = "administrador")]
[Route("api/empresas")]
public class EmpresaMantenimientoController : Controller
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
