using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Contrato;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/contratos")]
public class ContratoController : Controller
{
    private readonly ContratoService _contratoService;

    public ContratoController(ContratoService contratoService) =>
        _contratoService = contratoService;

    [HttpPost]
    [Authorize(Roles = "administrador")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ContractHtmlProcessor.MaxHtmlLength + 64_000)]
    public async Task<IActionResult> Create(
        [FromForm] int? prestamoId,
        IFormFile? archivo,
        CancellationToken cancellationToken
    )
    {
        if (archivo == null || archivo.Length == 0)
            return InvalidUpload("Archivo HTML requerido");

        if (archivo.Length > ContractHtmlProcessor.MaxHtmlLength)
            return InvalidUpload("El contrato supera el tamaño máximo permitido");

        if (
            !string.Equals(archivo.ContentType, "text/html", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                Path.GetExtension(archivo.FileName),
                ".html",
                StringComparison.OrdinalIgnoreCase
            )
        )
            return InvalidUpload("El contrato debe ser un archivo HTML");

        using var reader = new StreamReader(archivo.OpenReadStream());
        var htmlContent = await reader.ReadToEndAsync(cancellationToken);

        var result = await _contratoService.CreateForPrestamo(
            prestamoId ?? 0,
            htmlContent,
            cancellationToken
        );

        return ToResponse(result);
    }

    [HttpGet("{prestamoId:int}")]
    public async Task<IActionResult> GetByPrestamoId(
        int prestamoId,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
            await _contratoService.GetByPrestamoId(
                prestamoId,
                User.Identity?.Name ?? string.Empty,
                User.IsInRole("administrador"),
                cancellationToken
            )
        );

    [Authorize(Roles = "administrador")]
    [HttpDelete("{prestamoId:int}")]
    public async Task<IActionResult> Delete(int prestamoId) =>
        ToDeleteResponse(await _contratoService.Delete(prestamoId));

    private IActionResult InvalidUpload(string message) =>
        ToResponse(
            Result<ContratoDto>.Invalid(
                new ValidationError { Identifier = "archivo", ErrorMessage = message }
            )
        );
}
