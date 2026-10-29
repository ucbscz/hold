using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Prestamo.State;
using IMT_Reservas.Server.Core.Entities;
using Riok.Mapperly.Abstractions;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;

namespace IMT_Reservas.Server.Application.Features.Prestamo;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName, EnumMappingIgnoreCase = true)]
public partial class PrestamoMapper : IMapper<PrestamoEntity, PrestamoDto>
{
    [MapProperty(nameof(PrestamoEntity.Carnet), nameof(PrestamoDto.CarnetUsuario))]
    [MapperIgnoreTarget(nameof(PrestamoDto.ApellidoPaternoUsuario))]
    [MapperIgnoreTarget(nameof(PrestamoDto.CodigoImt))]
    [MapperIgnoreTarget(nameof(PrestamoDto.Contrato))]
    [MapperIgnoreTarget(nameof(PrestamoDto.GrupoEquipoId))]
    [MapperIgnoreTarget(nameof(PrestamoDto.NombreGavetero))]
    [MapperIgnoreTarget(nameof(PrestamoDto.NombreGrupoEquipo))]
    [MapperIgnoreTarget(nameof(PrestamoDto.NombreMueble))]
    [MapperIgnoreTarget(nameof(PrestamoDto.AdministradorAmbiente))]
    [MapperIgnoreTarget(nameof(PrestamoDto.NombreUsuario))]
    [MapperIgnoreTarget(nameof(PrestamoDto.TelefonoUsuario))]
    [MapperIgnoreTarget(nameof(PrestamoDto.UbicacionEquipo))]
    [MapperIgnoreTarget(nameof(PrestamoDto.UbicacionMueble))]
    [MapperIgnoreTarget(nameof(PrestamoDto.EstadoEquipo))]
    [MapperIgnoreTarget(nameof(PrestamoDto.EquiposRetorno))]
    public partial PrestamoDto ToDto(PrestamoEntity entity);

    [MapProperty(nameof(PrestamoDto.CarnetUsuario), nameof(PrestamoEntity.Carnet))]
    [MapperIgnoreSource(nameof(PrestamoDto.ApellidoPaternoUsuario))]
    [MapperIgnoreSource(nameof(PrestamoDto.CodigoImt))]
    [MapperIgnoreSource(nameof(PrestamoDto.Contrato))]
    [MapperIgnoreSource(nameof(PrestamoDto.GrupoEquipoId))]
    [MapperIgnoreSource(nameof(PrestamoDto.NombreGavetero))]
    [MapperIgnoreSource(nameof(PrestamoDto.NombreGrupoEquipo))]
    [MapperIgnoreSource(nameof(PrestamoDto.NombreMueble))]
    [MapperIgnoreSource(nameof(PrestamoDto.AdministradorAmbiente))]
    [MapperIgnoreSource(nameof(PrestamoDto.NombreUsuario))]
    [MapperIgnoreSource(nameof(PrestamoDto.TelefonoUsuario))]
    [MapperIgnoreSource(nameof(PrestamoDto.UbicacionEquipo))]
    [MapperIgnoreSource(nameof(PrestamoDto.UbicacionMueble))]
    [MapperIgnoreSource(nameof(PrestamoDto.EstadoEquipo))]
    [MapperIgnoreSource(nameof(PrestamoDto.EquiposRetorno))]
    public partial PrestamoEntity ToEntity(PrestamoDto dto);

    [MapperIgnoreSource(nameof(PrestamoDto.Contrato))]
    [MapProperty(nameof(PrestamoDto.CarnetUsuario), nameof(PrestamoEntity.Carnet))]
    public partial void Update(PrestamoDto source, PrestamoEntity destination);

    public partial IQueryable<PrestamoDto> ProjectTo(IQueryable<PrestamoEntity> source);

    private static string EstadoPrestamoToString(EstadoPrestamo estado) =>
        PrestamoState.ToText(estado);
}
