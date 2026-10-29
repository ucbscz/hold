using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Api;

[Authorize]
[Route("api/procedencias")]
public sealed class ProcedenciaController(
    Service<Procedencia, Repository<Procedencia, CatalogoInventarioDto>, CatalogoInventarioDto> service
) : InventoryCatalogController<Procedencia>(service);
