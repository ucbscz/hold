using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Mvc;
using MuebleEntity = UCB_Hold.Server.Modules.Inventario.Domain.Mueble;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Api;

[Route("api/muebles")]
public class MuebleController : CrudController<MuebleEntity, Repository<MuebleEntity, MuebleDto>, MuebleDto>
{
    public MuebleController(Service<MuebleEntity, Repository<MuebleEntity, MuebleDto>, MuebleDto> service)
        : base(service) { }
}
