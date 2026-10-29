using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/ambientes")]
public sealed class AmbienteController(
    Service<Ambiente, Repository<Ambiente, CatalogoInventarioDto>, CatalogoInventarioDto> service
) : InventoryCatalogController<Ambiente>(service);
