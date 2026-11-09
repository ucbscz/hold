using Riok.Mapperly.Abstractions;
using GrupoEquipoEntity = UCB_Hold.Server.Modules.Inventario.Domain.GrupoEquipo;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

[Mapper]
public partial class GrupoEquipoMapper : IMapper<GrupoEquipoEntity, GrupoEquipoDto>
{
    [MapProperty("Categoria.Nombre", nameof(GrupoEquipoDto.NombreCategoria))]
    [MapperIgnoreTarget(nameof(GrupoEquipoDto.CodigosCarrera))]
    public partial GrupoEquipoDto ToDto(GrupoEquipoEntity entity);

    [MapperIgnoreTarget("Categoria")]
    [MapperIgnoreSource(nameof(GrupoEquipoDto.NombreCategoria))]
    [MapperIgnoreSource(nameof(GrupoEquipoDto.CodigosCarrera))]
    public partial GrupoEquipoEntity ToEntity(GrupoEquipoDto dto);

    public partial IQueryable<GrupoEquipoDto> ProjectTo(IQueryable<GrupoEquipoEntity> source);
}
