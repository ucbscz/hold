using Riok.Mapperly.Abstractions;
using CategoriaEntity = IMT_Reservas.Server.Modules.Inventario.Domain.Categoria;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Application;

[Mapper]
public partial class CategoriaMapper : IMapper<CategoriaEntity, CategoriaDto>
{
    public partial CategoriaDto ToDto(CategoriaEntity entity);

    public partial CategoriaEntity ToEntity(CategoriaDto dto);

    public partial IQueryable<CategoriaDto> ProjectTo(IQueryable<CategoriaEntity> source);
}
