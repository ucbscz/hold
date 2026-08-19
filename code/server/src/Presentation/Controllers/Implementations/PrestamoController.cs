using IMT_Reservas.Server.Application.Features.Prestamo;
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
        [FromQuery] string? estado = null
    )
    {
        if (User.IsInRole("administrador"))
        {
            if (string.IsNullOrWhiteSpace(carnet) && string.IsNullOrWhiteSpace(estado))
                return ToResponse(await _service.GetAll());

            return ToResponse(await _service.GetFiltered(carnet, estado ?? string.Empty));
        }

        return ToResponse(
            await _service.GetHistory(User.Identity?.Name ?? string.Empty, estado ?? string.Empty)
        );
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [HttpGet("elegibilidad")]
    public async Task<IActionResult> GetReservationStatus() =>
        ToResponse(await _service.GetReservationStatus(User.Identity?.Name ?? string.Empty));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrestamoDto request)
    {
        var result = await _service.Create(request);
        return ToCreatedResponse(result, nameof(Get), new { id = result.Value?.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PrestamoDto dto) =>
        ToResponse(await _service.Update(id, dto));

    [Authorize(Roles = "administrador")]
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] PrestamoDto request
    ) =>
        ToResponse(
            await _service.UpdateStatus(
                id,
                request.EstadoPrestamo ?? string.Empty,
                request.Observacion,
                request,
                User.Identity?.Name
            )
        );

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));

}
