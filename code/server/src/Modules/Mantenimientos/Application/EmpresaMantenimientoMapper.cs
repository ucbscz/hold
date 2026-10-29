using Riok.Mapperly.Abstractions;
using EmpresaMantenimientoEntity = IMT_Reservas.Server.Modules.Mantenimientos.Domain.EmpresaMantenimiento;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Application;

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
