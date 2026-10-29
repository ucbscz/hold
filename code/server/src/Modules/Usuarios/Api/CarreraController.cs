using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarreraEntity = IMT_Reservas.Server.Modules.Usuarios.Domain.Carrera;
using IMT_Reservas.Server.Modules.Usuarios.Application;
using IMT_Reservas.Server.Shared.Kernel;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Usuarios.Api;

[Authorize]
[Route("api/carreras")]
public class CarreraController : ApiController
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

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CarreraDto dto)
    {
        var result = await _service.Create(dto);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CarreraDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));
}
