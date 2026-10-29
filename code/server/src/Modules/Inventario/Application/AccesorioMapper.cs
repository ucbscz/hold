using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using AccesorioEntity = IMT_Reservas.Server.Core.Entities.Accesorio;

namespace IMT_Reservas.Server.Application.Features.Accesorio;

[Mapper]
public partial class AccesorioMapper : IMapper<AccesorioEntity, AccesorioDto>
{
    [MapperIgnoreTarget(nameof(AccesorioDto.CodigoImtEquipoAsociado))]
    [MapperIgnoreTarget(nameof(AccesorioDto.NombreEquipoAsociado))]
    public partial AccesorioDto ToDto(AccesorioEntity entity);

    [MapperIgnoreSource(nameof(AccesorioDto.CodigoImtEquipoAsociado))]
    [MapperIgnoreSource(nameof(AccesorioDto.NombreEquipoAsociado))]
    public partial AccesorioEntity ToEntity(AccesorioDto dto);

    public partial IQueryable<AccesorioDto> ProjectTo(IQueryable<AccesorioEntity> source);
}
