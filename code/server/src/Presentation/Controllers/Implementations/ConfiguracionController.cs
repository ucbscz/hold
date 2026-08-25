using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Presentation.Controllers.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Route("api/[controller]")]
public class ConfiguracionController : Controller
{
    private readonly ConfiguracionService _service;

    public ConfiguracionController(ConfiguracionService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var result = await _service.GetConfiguracion();
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Update([FromBody] ConfiguracionDto dto)
    {
        var result = await _service.UpdateConfiguracion(dto);
        return ToResponse(result);
    }
}
