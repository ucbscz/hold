using Riok.Mapperly.Abstractions;
using GrupoEquipoEntity = IMT_Reservas.Server.Modules.Inventario.Domain.GrupoEquipo;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Application;

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
