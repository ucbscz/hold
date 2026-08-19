using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Application.Features.Prestamo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/prestamo")]
public class PrestamoController : Controller
{
    private readonly PrestamoService _service;
    private readonly ContratoService _contratoService;

    public PrestamoController(PrestamoService service, ContratoService contratoService)
    {
        _service = service;
        _contratoService = contratoService;
    }

    [Authorize(Roles = "administrador")]
    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResponse(await _service.GetAll());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => ToResponse(await _service.Get(id));

    [HttpGet("estado-reserva")]
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
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] string estado,
        [FromQuery] string? observacion = null,
        [FromBody] PrestamoDto? body = null
    ) => ToResponse(await _service.UpdateStatus(id, estado, observacion, body));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => ToDeleteResponse(await _service.Delete(id));

    [HttpGet("historial")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string carnetUsuario,
        [FromQuery] string estadoPrestamo
    ) => ToResponse(await _service.GetHistory(carnetUsuario, estadoPrestamo));

    [Authorize(Roles = "administrador")]
    [HttpGet("por-usuario/{carnet}")]
    public async Task<IActionResult> GetByUsuario(string carnet) =>
        ToResponse(await _service.GetHistory(carnet, string.Empty));

    [HttpGet("contrato/{prestamoId}")]
    public async Task<IActionResult> GetContrato(int prestamoId)
    {
        var result = await _contratoService.GetByPrestamoId(prestamoId);

        if (!result.IsSuccess)
            return ToResponse(result);

        return Ok(new { contrato = result.Value!.ContratoHtml });
    }
}
