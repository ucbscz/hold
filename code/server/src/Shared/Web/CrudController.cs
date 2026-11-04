using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Shared.Web;

[Authorize]
public abstract class CrudController<TEntity, TRepository, TDto> : ApiController
    where TEntity : Entity
    where TRepository : Repository<TEntity, TDto>
    where TDto : class, IIdentifiedDto
{
    private readonly Service<TEntity, TRepository, TDto> _service;

    protected CrudController(Service<TEntity, TRepository, TDto> service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TDto dto)
    {
        var result = await _service.Create(dto);

        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
