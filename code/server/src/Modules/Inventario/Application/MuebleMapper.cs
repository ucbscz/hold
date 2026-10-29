using Riok.Mapperly.Abstractions;
using MuebleEntity = UCB_Hold.Server.Modules.Inventario.Domain.Mueble;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

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
