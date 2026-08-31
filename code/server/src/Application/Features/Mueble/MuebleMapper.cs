using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using MuebleEntity = IMT_Reservas.Server.Core.Entities.Mueble;

namespace IMT_Reservas.Server.Application.Features.Mueble;

[Mapper]
public partial class MuebleMapper : IMapper<MuebleEntity, MuebleDto>
{
    [MapProperty("Ambiente.Nombre", nameof(MuebleDto.NombreAmbiente))]
    public partial MuebleDto ToDto(MuebleEntity entity);

    [MapperIgnoreSource(nameof(MuebleDto.NombreAmbiente))]
    [MapperIgnoreTarget(nameof(MuebleEntity.Ambiente))]
    public partial MuebleEntity ToEntity(MuebleDto dto);

    public partial IQueryable<MuebleDto> ProjectTo(IQueryable<MuebleEntity> source);
}
