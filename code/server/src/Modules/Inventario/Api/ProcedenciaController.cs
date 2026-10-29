using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Inventario;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMT_Reservas.Server.Presentation.Controllers.Abstraction;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

[Authorize]
[Route("api/procedencias")]
public sealed class ProcedenciaController(
    Service<Procedencia, Repository<Procedencia, CatalogoInventarioDto>, CatalogoInventarioDto> service
) : InventoryCatalogController<Procedencia>(service);
