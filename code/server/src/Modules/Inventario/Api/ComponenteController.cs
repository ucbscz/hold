using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Mvc;
using ComponenteEntity = UCB_Hold.Server.Modules.Inventario.Domain.Componente;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Api;

[Route("api/componentes")]
public class ComponenteController : CrudController<ComponenteEntity, ComponenteRepository, ComponenteDto>
{
    public ComponenteController(Service<ComponenteEntity, ComponenteRepository, ComponenteDto> service)
        : base(service) { }
}
