using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmpresaMantenimientoEntity = UCB_Hold.Server.Modules.Mantenimientos.Domain.EmpresaMantenimiento;
using UCB_Hold.Server.Modules.Mantenimientos.Application;
using UCB_Hold.Server.Modules.Mantenimientos.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Mantenimientos.Api;

[Authorize(Roles = Permisos.Administrador)]
[Route("api/empresas")]
public class EmpresaMantenimientoController
    : CrudController<EmpresaMantenimientoEntity, EmpresaMantenimientoRepository, EmpresaMantenimientoDto>
{
    public EmpresaMantenimientoController(
        Service<EmpresaMantenimientoEntity, EmpresaMantenimientoRepository, EmpresaMantenimientoDto> service
    )
        : base(service) { }
}
