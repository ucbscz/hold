using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using ContratoEntity = IMT_Reservas.Server.Core.Entities.Contrato;

namespace IMT_Reservas.Server.Application.Features.Contrato;

[Mapper]
public partial class ContratoMapper : IMapper<ContratoEntity, ContratoDto>
{
    public partial ContratoDto ToDto(ContratoEntity entity);

    public partial ContratoEntity ToEntity(ContratoDto dto);

    public partial IQueryable<ContratoDto> ProjectTo(IQueryable<ContratoEntity> source);
}
