using Riok.Mapperly.Abstractions;
using ContratoEntity = IMT_Reservas.Server.Modules.Prestamos.Domain.Contrato;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Prestamos.Application;

[Mapper]
public partial class ContratoMapper : IMapper<ContratoEntity, ContratoDto>
{
    public partial ContratoDto ToDto(ContratoEntity entity);

    public partial ContratoEntity ToEntity(ContratoDto dto);

    public partial IQueryable<ContratoDto> ProjectTo(IQueryable<ContratoEntity> source);
}
