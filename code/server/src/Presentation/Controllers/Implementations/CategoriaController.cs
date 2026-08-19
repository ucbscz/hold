using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Categoria;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CategoriaEntity = IMT_Reservas.Server.Core.Entities.Categoria;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/categoria")]
public class CategoriaController : Controller
{
    private readonly Service<
        CategoriaEntity,
        Repository<CategoriaEntity, CategoriaDto>,
        CategoriaDto
    > _service;

    public CategoriaController(
        Service<CategoriaEntity, Repository<CategoriaEntity, CategoriaDto>, CategoriaDto> service
    ) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = "administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoriaDto dto)
    {
        var result = await _service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = "administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoriaDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = "administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
