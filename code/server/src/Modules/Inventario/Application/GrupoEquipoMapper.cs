using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using GrupoEquipoEntity = IMT_Reservas.Server.Core.Entities.GrupoEquipo;

namespace IMT_Reservas.Server.Application.Features.GrupoEquipo;

[Mapper]
public partial class GrupoEquipoMapper : IMapper<GrupoEquipoEntity, GrupoEquipoDto>
{
    [MapProperty("Categoria.Nombre", nameof(GrupoEquipoDto.NombreCategoria))]
    public partial GrupoEquipoDto ToDto(GrupoEquipoEntity entity);

    [MapperIgnoreTarget("Categoria")]
    [MapperIgnoreSource(nameof(GrupoEquipoDto.NombreCategoria))]
    public partial GrupoEquipoEntity ToEntity(GrupoEquipoDto dto);

    public partial IQueryable<GrupoEquipoDto> ProjectTo(IQueryable<GrupoEquipoEntity> source);
}
