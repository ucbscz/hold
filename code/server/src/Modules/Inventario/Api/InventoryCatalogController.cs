using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Security;
using UCB_Hold.Server.Shared.Web;

namespace UCB_Hold.Server.Modules.Inventario.Api;

public abstract class InventoryCatalogController<TEntity>(
    Service<TEntity, Repository<TEntity, CatalogoInventarioDto>, CatalogoInventarioDto> service
) : ApiController
    where TEntity : Entity
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await service.Get(id));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create(CatalogoInventarioDto dto)
    {
        var result = await service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CatalogoInventarioDto dto) =>
        ToResponse(await service.Update(id, dto));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToDeleteResponse(await service.Delete(id));
}
