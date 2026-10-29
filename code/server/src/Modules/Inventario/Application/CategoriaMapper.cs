using Riok.Mapperly.Abstractions;
using CategoriaEntity = UCB_Hold.Server.Modules.Inventario.Domain.Categoria;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

[Mapper]
public partial class CategoriaMapper : IMapper<CategoriaEntity, CategoriaDto>
{
    public partial CategoriaDto ToDto(CategoriaEntity entity);

    public partial CategoriaEntity ToEntity(CategoriaDto dto);

    public partial IQueryable<CategoriaDto> ProjectTo(IQueryable<CategoriaEntity> source);
}
