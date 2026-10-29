using Riok.Mapperly.Abstractions;
using AccesorioEntity = IMT_Reservas.Server.Modules.Inventario.Domain.Accesorio;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Application;

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
