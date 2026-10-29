using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using CategoriaEntity = IMT_Reservas.Server.Core.Entities.Categoria;

namespace IMT_Reservas.Server.Application.Features.Categoria;

[Mapper]
public partial class CategoriaMapper : IMapper<CategoriaEntity, CategoriaDto>
{
    public partial CategoriaDto ToDto(CategoriaEntity entity);

    public partial CategoriaEntity ToEntity(CategoriaDto dto);

    public partial IQueryable<CategoriaDto> ProjectTo(IQueryable<CategoriaEntity> source);
}
