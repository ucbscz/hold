using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Carrera;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarreraEntity = IMT_Reservas.Server.Core.Entities.Carrera;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/carrera")]
public class CarreraController : Controller
{
    private readonly Service<
        CarreraEntity,
        Repository<CarreraEntity, CarreraDto>,
        CarreraDto
    > _service;

    public CarreraController(
        Service<CarreraEntity, Repository<CarreraEntity, CarreraDto>, CarreraDto> service
    ) => _service = service;

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = "administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CarreraDto dto)
    {
        var result = await _service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = "administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CarreraDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = "administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
