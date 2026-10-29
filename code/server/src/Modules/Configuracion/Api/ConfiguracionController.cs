using IMT_Reservas.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using IMT_Reservas.Server.Modules.Configuracion.Application;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Configuracion.Api;

[Route("api/configuracion")]
public class ConfiguracionController : ApiController
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
        Response.Headers.CacheControl = CacheControlHeaderValue.NoStoreString;
        return Ok(
            User.IsInRole(Permisos.Administrador)
                ? result
                : ConfiguracionPublicaDto.From(result)
        );
    }

    [HttpGet("responsables")]
    [Authorize(Roles = Permisos.Administrador)]
    public async Task<IActionResult> Responsables([FromQuery] string? buscar, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = CacheControlHeaderValue.NoStoreString;
        return ToResponse(await _service.BuscarResponsables(buscar, cancellationToken));
    }

    [HttpPut]
    [Authorize(Roles = Permisos.Administrador)]
    public async Task<IActionResult> Update(
        [FromBody] ConfiguracionDto dto,
        CancellationToken cancellationToken
    )
    {
        var result = await _service.UpdateConfiguracion(dto, cancellationToken);
        return ToResponse(result);
    }
}
