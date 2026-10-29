using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CategoriaEntity = IMT_Reservas.Server.Modules.Inventario.Domain.Categoria;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Shared.Kernel;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/categorias")]
public class CategoriaController : ApiController
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

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoriaDto dto)
    {
        var result = await _service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoriaDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
