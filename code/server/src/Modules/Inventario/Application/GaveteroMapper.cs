using Riok.Mapperly.Abstractions;
using GaveteroEntity = UCB_Hold.Server.Modules.Inventario.Domain.Gavetero;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

[Mapper]
public partial class GaveteroMapper : IMapper<GaveteroEntity, GaveteroDto>
{
    [MapProperty("Mueble.Nombre", nameof(GaveteroDto.NombreMueble))]
    public partial GaveteroDto ToDto(GaveteroEntity entity);

    [MapperIgnoreTarget("Mueble")]
    [MapperIgnoreSource(nameof(GaveteroDto.NombreMueble))]
    public partial GaveteroEntity ToEntity(GaveteroDto dto);

    public partial IQueryable<GaveteroDto> ProjectTo(IQueryable<GaveteroEntity> source);
}
