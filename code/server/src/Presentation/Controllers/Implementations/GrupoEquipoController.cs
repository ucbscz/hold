using IMT_Reservas.Server.Application.Features.GrupoEquipo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/grupos")]
public class GrupoEquipoController : Controller
{
    private readonly GrupoEquipoService _service;
    private readonly ComentarioEquipoService _comentarios;

    public GrupoEquipoController(GrupoEquipoService service, ComentarioEquipoService comentarios)
    {
        _service = service;
        _comentarios = comentarios;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] string? categoria = null
    ) =>
        string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(categoria)
            ? ToResponse(await _service.GetAll())
            : ToResponse(await _service.Search(nombre, categoria));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [HttpGet("{id:int}/comentarios")]
    public async Task<IActionResult> GetComentarios(
        int id,
        [FromQuery] string? orden = null
    ) => ToResponse(await _comentarios.GetByGrupo(id, orden, CurrentCarnet, IsAdmin));

    [HttpPost("{id:int}/comentarios")]
    public async Task<IActionResult> CreateComentario(
        int id,
        [FromBody] CrearComentarioEquipoDto? dto
    ) => ToResponse(await _comentarios.Create(id, CurrentCarnet, dto));

    [HttpPost("{id:int}/comentarios/{comentarioId:int}/likes")]
    public async Task<IActionResult> ToggleComentarioLike(int id, int comentarioId) =>
        ToResponse(await _comentarios.ToggleLike(id, comentarioId, CurrentCarnet, IsAdmin));

    [HttpDelete("{id:int}/comentarios/{comentarioId:int}")]
    public async Task<IActionResult> DeleteComentario(int id, int comentarioId)
    {
        var result = await _comentarios.Delete(id, comentarioId, CurrentCarnet, IsAdmin);

        return result.IsSuccess ? NoContent() : ToResponse(result);
    }

    [Authorize(Roles = "administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GrupoEquipoDto dto)
    {
        var result = await _service.Create(dto);

        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [Authorize(Roles = "administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GrupoEquipoDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = "administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));

    private string CurrentCarnet => User.Identity?.Name ?? string.Empty;
    private bool IsAdmin => User.IsInRole("administrador");
}
