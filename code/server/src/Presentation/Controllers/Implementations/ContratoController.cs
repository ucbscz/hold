using IMT_Reservas.Server.Application.Features.Contrato;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/contrato")]
public class ContratoController : Controller
{
    private readonly ContratoService _contratoService;

    public ContratoController(ContratoService contratoService) =>
        _contratoService = contratoService;

    [HttpPost("crear")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] int? prestamoId, IFormFile? archivo)
    {
        string? htmlContent = null;

        if (archivo != null)
        {
            using var reader = new StreamReader(archivo.OpenReadStream());
            htmlContent = await reader.ReadToEndAsync();
        }

        var result = await _contratoService.CreateForPrestamo(prestamoId ?? 0, htmlContent ?? "");

        return ToResponse(result);
    }

    [HttpGet("{prestamoId}")]
    public async Task<IActionResult> GetByPrestamoId(int prestamoId) =>
        ToResponse(await _contratoService.GetByPrestamoId(prestamoId));

    [HttpDelete("{prestamoId}")]
    public async Task<IActionResult> Delete(int prestamoId) =>
        ToDeleteResponse(await _contratoService.Delete(prestamoId));
}
