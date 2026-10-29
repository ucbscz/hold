using IMT_Reservas.Server.Application.Abstraction;
using Riok.Mapperly.Abstractions;
using EmpresaMantenimientoEntity = IMT_Reservas.Server.Core.Entities.EmpresaMantenimiento;

namespace IMT_Reservas.Server.Application.Features.EmpresaMantenimiento;

[Mapper]
public partial class EmpresaMantenimientoMapper
    : IMapper<EmpresaMantenimientoEntity, EmpresaMantenimientoDto>
{
    [MapProperty(
        nameof(EmpresaMantenimientoEntity.Nombre),
        nameof(EmpresaMantenimientoDto.NombreEmpresa)
    )]
    public partial EmpresaMantenimientoDto ToDto(EmpresaMantenimientoEntity entity);

    [MapProperty(
        nameof(EmpresaMantenimientoDto.NombreEmpresa),
        nameof(EmpresaMantenimientoEntity.Nombre)
    )]
    public partial EmpresaMantenimientoEntity ToEntity(EmpresaMantenimientoDto dto);

    public partial IQueryable<EmpresaMantenimientoDto> ProjectTo(
        IQueryable<EmpresaMantenimientoEntity> source
    );
}
