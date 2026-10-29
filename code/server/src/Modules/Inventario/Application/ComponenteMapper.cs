using Riok.Mapperly.Abstractions;
using ComponenteEntity = UCB_Hold.Server.Modules.Inventario.Domain.Componente;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

[Mapper]
public partial class ComponenteMapper : IMapper<ComponenteEntity, ComponenteDto>
{
    [MapperIgnoreTarget(nameof(ComponenteDto.CodigoImtEquipo))]
    [MapperIgnoreTarget(nameof(ComponenteDto.NombreEquipo))]
    public partial ComponenteDto ToDto(ComponenteEntity entity);

    [MapperIgnoreSource(nameof(ComponenteDto.CodigoImtEquipo))]
    [MapperIgnoreSource(nameof(ComponenteDto.NombreEquipo))]
    public partial ComponenteEntity ToEntity(ComponenteDto dto);

    public partial IQueryable<ComponenteDto> ProjectTo(IQueryable<ComponenteEntity> source);
}
