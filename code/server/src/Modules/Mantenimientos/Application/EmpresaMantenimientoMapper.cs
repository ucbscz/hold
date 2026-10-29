using Riok.Mapperly.Abstractions;
using EmpresaMantenimientoEntity = UCB_Hold.Server.Modules.Mantenimientos.Domain.EmpresaMantenimiento;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Mantenimientos.Application;

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
