using IMT_Reservas.Server.Application.Features.Configuracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Controller = IMT_Reservas.Server.Presentation.Controllers.Abstraction.Controller;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Route("api/configuracion")]
public class ConfiguracionController : Controller
{
    private readonly ConfiguracionService _service;

    public ConfiguracionController(ConfiguracionService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _service.GetConfiguracion(cancellationToken);
        if (!User.IsInRole("administrador")) result.CarnetJefeCarrera = null;
        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }

    [HttpGet("responsables")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Responsables([FromQuery] string? buscar, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (buscar?.Length > 100) return BadRequest("La búsqueda no puede superar 100 caracteres.");
        return Ok(await _service.BuscarResponsables(buscar, cancellationToken));
    }

    [HttpPut]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Update(
        [FromBody] ConfiguracionDto dto,
        CancellationToken cancellationToken
    )
    {
        var result = await _service.UpdateConfiguracion(dto, cancellationToken);
        return ToResponse(result);
    }
}
