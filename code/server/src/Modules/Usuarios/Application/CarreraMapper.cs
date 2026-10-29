using Riok.Mapperly.Abstractions;
using CarreraEntity = UCB_Hold.Server.Modules.Usuarios.Domain.Carrera;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

[Mapper]
public partial class CarreraMapper : IMapper<CarreraEntity, CarreraDto>
{
    public partial CarreraDto ToDto(CarreraEntity entity);

    public partial CarreraEntity ToEntity(CarreraDto dto);

    public partial IQueryable<CarreraDto> ProjectTo(IQueryable<CarreraEntity> source);
}
