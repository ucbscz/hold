using Riok.Mapperly.Abstractions;
using CarreraEntity = IMT_Reservas.Server.Modules.Usuarios.Domain.Carrera;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Usuarios.Application;

[Mapper]
public partial class CarreraMapper : IMapper<CarreraEntity, CarreraDto>
{
    public partial CarreraDto ToDto(CarreraEntity entity);

    public partial CarreraEntity ToEntity(CarreraDto dto);

    public partial IQueryable<CarreraDto> ProjectTo(IQueryable<CarreraEntity> source);
}
