using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarreraEntity = UCB_Hold.Server.Modules.Usuarios.Domain.Carrera;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Usuarios.Api;

[Route("api/carreras")]
public class CarreraController
    : CrudController<CarreraEntity, Repository<CarreraEntity, CarreraDto>, CarreraDto>
{
    public CarreraController(
        Service<CarreraEntity, Repository<CarreraEntity, CarreraDto>, CarreraDto> service
    )
        : base(service) { }

    [AllowAnonymous]
    public override Task<IActionResult> GetAll() => base.GetAll();
}
