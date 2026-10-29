using Riok.Mapperly.Abstractions;
using ContratoEntity = UCB_Hold.Server.Modules.Prestamos.Domain.Contrato;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

[Mapper]
public partial class ContratoMapper : IMapper<ContratoEntity, ContratoDto>
{
    public partial ContratoDto ToDto(ContratoEntity entity);

    public partial ContratoEntity ToEntity(ContratoDto dto);

    public partial IQueryable<ContratoDto> ProjectTo(IQueryable<ContratoEntity> source);
}
