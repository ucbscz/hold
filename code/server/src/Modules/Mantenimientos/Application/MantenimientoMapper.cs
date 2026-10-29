using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using MantenimientoEntity = IMT_Reservas.Server.Core.Entities.Mantenimiento;

namespace IMT_Reservas.Server.Application.Features.Mantenimiento;

[Mapper]
public partial class MantenimientoMapper : IMapper<MantenimientoEntity, MantenimientoDto>
{
    [MapperIgnoreTarget(nameof(MantenimientoDto.CodigoImt))]
    [MapperIgnoreTarget(nameof(MantenimientoDto.CodigoImtEquipo))]
    [MapperIgnoreTarget(nameof(MantenimientoDto.DescripcionEquipo))]
    [MapperIgnoreTarget(nameof(MantenimientoDto.DescripcionesEquipo))]
    [MapperIgnoreTarget(nameof(MantenimientoDto.NombreEmpresaMantenimiento))]
    [MapperIgnoreTarget(nameof(MantenimientoDto.NombreGrupoEquipo))]
    [MapperIgnoreTarget(nameof(MantenimientoDto.TipoMantenimiento))]
    [MapperIgnoreTarget(nameof(MantenimientoDto.TiposMantenimiento))]
    public partial MantenimientoDto ToDto(MantenimientoEntity entity);

    [MapperIgnoreSource(nameof(MantenimientoDto.CodigoImt))]
    [MapperIgnoreSource(nameof(MantenimientoDto.CodigoImtEquipo))]
    [MapperIgnoreSource(nameof(MantenimientoDto.DescripcionEquipo))]
    [MapperIgnoreSource(nameof(MantenimientoDto.DescripcionesEquipo))]
    [MapperIgnoreSource(nameof(MantenimientoDto.NombreEmpresaMantenimiento))]
    [MapperIgnoreSource(nameof(MantenimientoDto.NombreGrupoEquipo))]
    [MapperIgnoreSource(nameof(MantenimientoDto.TipoMantenimiento))]
    [MapperIgnoreSource(nameof(MantenimientoDto.TiposMantenimiento))]
    public partial MantenimientoEntity ToEntity(MantenimientoDto dto);

    public partial IQueryable<MantenimientoDto> ProjectTo(IQueryable<MantenimientoEntity> source);
}
