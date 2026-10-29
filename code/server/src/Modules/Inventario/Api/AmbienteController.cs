using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/ambientes")]
public sealed class AmbienteController(
    Service<Ambiente, Repository<Ambiente, CatalogoInventarioDto>, CatalogoInventarioDto> service
) : InventoryCatalogController<Ambiente>(service);
