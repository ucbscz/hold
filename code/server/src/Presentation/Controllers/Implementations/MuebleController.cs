using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Mueble;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;
using MuebleEntity = IMT_Reservas.Server.Core.Entities.Mueble;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/muebles")]
public class MuebleController : Controller
{
    private readonly Service<MuebleEntity, Repository<MuebleEntity, MuebleDto>, MuebleDto> _service;

    public MuebleController(
        Service<MuebleEntity, Repository<MuebleEntity, MuebleDto>, MuebleDto> service
    ) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = "administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MuebleDto dto)
    {
        var result = await _service.Create(dto);

        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = "administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MuebleDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = "administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
