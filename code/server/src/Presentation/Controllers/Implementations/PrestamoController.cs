using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/prestamos")]
public class PrestamoController : Controller
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
        {
            if (string.IsNullOrWhiteSpace(carnet) && string.IsNullOrWhiteSpace(estado))
                return ToResponse(
                    await _service.GetFiltered(
                        null,
                        string.Empty,
                        pagina,
                        tamanoPagina,
                        cancellationToken,
                        guardado
                    )
                );

            return ToResponse(
                await _service.GetFiltered(
                    carnet,
                    estado ?? string.Empty,
                    pagina,
                    tamanoPagina,
                    cancellationToken,
                    guardado
                )
            );
        }

        return ToResponse(
            await _service.GetHistory(
                User.Identity?.Name ?? string.Empty,
                estado ?? string.Empty,
                pagina,
                tamanoPagina,
                cancellationToken,
                guardado
            )
        );
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        ToResponse(
            await _service.GetAuthorized(
                id,
                User.Identity?.Name ?? string.Empty,
                User.PuedeGestionar(),
                cancellationToken
            )
        );

    [HttpGet("elegibilidad")]
    public async Task<IActionResult> GetReservationStatus(CancellationToken cancellationToken) =>
        ToResponse(
            await _service.GetReservationStatus(
                User.Identity?.Name ?? string.Empty,
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
            User.Identity?.Name ?? string.Empty,
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
        if (!User.PuedeGestionar())
        {
            if (!string.Equals(request.EstadoPrestamo, "cancelado", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            return ToResponse(await _service.CancelForUser(id, User.Identity?.Name ?? string.Empty, cancellationToken));
        }
        return ToResponse(
            await _service.UpdateStatus(
                id,
                request.EstadoPrestamo ?? string.Empty,
                request.Observacion,
                request,
                User.Identity?.Name,
                cancellationToken
            )
        );
    }

    [Authorize(Roles = "administrador")]
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
            User.Identity?.Name ?? string.Empty,
            dto.Guardado,
            cancellationToken
        )
    );

}
