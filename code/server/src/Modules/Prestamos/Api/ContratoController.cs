using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Prestamos.Api;

[Authorize]
[Route("api/contratos")]
public class ContratoController : ApiController
{
    private readonly ContratoService _contratoService;

    public ContratoController(ContratoService contratoService) =>
        _contratoService = contratoService;

    [HttpPost]
    [Authorize(Roles = Permisos.Gestion)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ContractHtmlProcessor.MaxHtmlLength + 64_000)]
    public async Task<IActionResult> Create(
        [FromForm] int? prestamoId,
        IFormFile? archivo,
        CancellationToken cancellationToken
    )
    {
        await using var content = archivo?.OpenReadStream() ?? Stream.Null;
        return ToResponse(
            await _contratoService.CreateFromFile(
                prestamoId ?? 0,
                archivo?.FileName,
                archivo?.ContentType,
                archivo?.Length ?? 0,
                content,
                cancellationToken
            )
        );
    }

    [HttpGet("{prestamoId:int}")]
    public async Task<IActionResult> GetByPrestamoId(
        int prestamoId,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
            await _contratoService.GetByPrestamoId(
                prestamoId,
                CurrentCarnet,
                User.PuedeGestionar(),
                cancellationToken
            )
        );

    [Authorize(Roles = Permisos.Administrador)]
    [HttpDelete("{prestamoId:int}")]
    public async Task<IActionResult> Delete(int prestamoId) =>
        ToDeleteResponse(await _contratoService.Delete(prestamoId));
}
