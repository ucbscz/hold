using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using CarreraEntity = IMT_Reservas.Server.Core.Entities.Carrera;

namespace IMT_Reservas.Server.Application.Features.Carrera;

[Mapper]
public partial class CarreraMapper : IMapper<CarreraEntity, CarreraDto>
{
    public partial CarreraDto ToDto(CarreraEntity entity);

    public partial CarreraEntity ToEntity(CarreraDto dto);

    public partial IQueryable<CarreraDto> ProjectTo(IQueryable<CarreraEntity> source);
}
