using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/procedencias")]
public sealed class ProcedenciaController(
    Service<Procedencia, Repository<Procedencia, CatalogoInventarioDto>, CatalogoInventarioDto> service
) : InventoryCatalogController<Procedencia>(service);
