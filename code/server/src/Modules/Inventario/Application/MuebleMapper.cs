using Riok.Mapperly.Abstractions;
using MuebleEntity = IMT_Reservas.Server.Modules.Inventario.Domain.Mueble;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Application;

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
