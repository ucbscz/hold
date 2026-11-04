using UCB_Hold.Server.Shared.Web;
using Microsoft.AspNetCore.Mvc;
using CategoriaEntity = UCB_Hold.Server.Modules.Inventario.Domain.Categoria;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Api;

[Route("api/categorias")]
public class CategoriaController : CrudController<CategoriaEntity, Repository<CategoriaEntity, CategoriaDto>, CategoriaDto>
{
    public CategoriaController(Service<CategoriaEntity, Repository<CategoriaEntity, CategoriaDto>, CategoriaDto> service)
        : base(service) { }
}
