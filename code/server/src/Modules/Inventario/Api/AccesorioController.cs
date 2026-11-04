using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Mvc;
using AccesorioEntity = UCB_Hold.Server.Modules.Inventario.Domain.Accesorio;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Api;

[Route("api/accesorios")]
public class AccesorioController : CrudController<AccesorioEntity, AccesorioRepository, AccesorioDto>
{
    public AccesorioController(Service<AccesorioEntity, AccesorioRepository, AccesorioDto> service)
        : base(service) { }
}
