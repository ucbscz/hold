using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Prestamos.Api;

[Authorize]
[Route("api/prestamos")]
public class PrestamoController : ApiController
{
    private readonly PrestamoService _service;

    public PrestamoController(PrestamoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? carnet = null,
        [FromQuery] string? estado = null,
        [FromQuery] bool? guardado = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = PrestamoReadRepository.MaxPageSize,
        CancellationToken cancellationToken = default
    )
    {
        if (User.PuedeGestionar())
            return ToResponse(
                await _service.GetFiltered(
                    string.IsNullOrWhiteSpace(carnet) ? null : carnet,
                    string.IsNullOrWhiteSpace(estado) ? string.Empty : estado,
                    pagina,
                    tamanoPagina,
                    guardado,
                    cancellationToken
                )
            );

        return ToResponse(
            await _service.GetHistory(
                CurrentCarnet,
                estado ?? string.Empty,
                pagina,
                tamanoPagina,
                guardado,
                cancellationToken
            )
        );
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        ToResponse(
            await _service.GetAuthorized(
                id,
                CurrentCarnet,
                User.PuedeGestionar(),
                cancellationToken
            )
        );

    [HttpGet("elegibilidad")]
    public async Task<IActionResult> GetReservationStatus(CancellationToken cancellationToken) =>
        ToResponse(
            await _service.GetReservationStatus(
                CurrentCarnet,
                cancellationToken
            )
        );

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] PrestamoDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _service.CreateForUser(
            request,
            CurrentCarnet,
            cancellationToken
        );
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] PrestamoDto request,
        CancellationToken cancellationToken
    )
    {
        return ToResponse(
            await _service.ChangeStatus(
                id,
                request,
                CurrentCarnet,
                User.PuedeGestionar(),
                cancellationToken
            )
        );
    }

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));

    [Authorize(Roles = Permisos.Gestion)]
    [HttpPatch("{id:int}/observacion")]
    public async Task<IActionResult> UpdateObservation(int id, [FromBody] PrestamoDto dto, CancellationToken token) =>
        ToResponse(await _service.UpdateObservation(id, dto.Observacion, token));

    [HttpPatch("{id:int}/guardado")]
    public async Task<IActionResult> SetSaved(
        int id,
        [FromBody] PrestamoDto dto,
        CancellationToken cancellationToken
    ) => ToResponse(
        await _service.SetSaved(
            id,
            CurrentCarnet,
            dto.Guardado,
            cancellationToken
        )
    );

}
