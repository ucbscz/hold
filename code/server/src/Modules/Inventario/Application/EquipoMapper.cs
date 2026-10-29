using Riok.Mapperly.Abstractions;
using EquipoEntity = UCB_Hold.Server.Modules.Inventario.Domain.Equipo;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName, EnumMappingIgnoreCase = true)]
public partial class EquipoMapper : IMapper<EquipoEntity, EquipoDto>
{
    [MapProperty("GrupoEquipo.Nombre", nameof(EquipoDto.NombreGrupoEquipo))]
    [MapProperty("Gavetero.Nombre", nameof(EquipoDto.NombreGavetero))]
    [MapPropertyFromSource(nameof(EquipoDto.Ubicacion), Use = nameof(UbicacionEquipo))]
    [MapProperty("Procedencia.Nombre", nameof(EquipoDto.Procedencia))]
    public partial EquipoDto ToDto(EquipoEntity entity);

    [MapperIgnoreTarget("Gavetero")]
    [MapperIgnoreTarget("GrupoEquipo")]
    [MapperIgnoreTarget("Ambiente")]
    [MapperIgnoreTarget("Procedencia")]
    [MapperIgnoreSource(nameof(EquipoDto.Ubicacion))]
    [MapperIgnoreSource(nameof(EquipoDto.Procedencia))]
    [MapperIgnoreSource(nameof(EquipoDto.NombreGavetero))]
    [MapperIgnoreSource(nameof(EquipoDto.NombreGrupoEquipo))]
    public partial EquipoEntity ToEntity(EquipoDto dto);

    public partial IQueryable<EquipoDto> ProjectTo(IQueryable<EquipoEntity> source);

    private static string? UbicacionEquipo(EquipoEntity equipo) =>
        equipo.Gavetero != null && equipo.Gavetero.Mueble != null && equipo.Gavetero.Mueble.Ambiente != null
            ? equipo.Gavetero.Mueble.Ambiente.Nombre
            : equipo.Ambiente != null ? equipo.Ambiente.Nombre : null;

    private static string EstadoEquipoToString(EstadoEquipo estado) =>
        EstadoEquipoNombre.ToName(estado);

    private static EstadoEquipo StringToEstadoEquipo(string? estado) =>
        EstadoEquipoNombre.Parse(estado);

    private static DateTime? DateOnlyToDateTime(DateOnly source) =>
        source.ToDateTime(TimeOnly.MinValue);

    private static DateOnly DateTimeToDateOnly(DateTime? source) =>
        source.HasValue ? DateOnly.FromDateTime(source.Value) : DateOnly.MinValue;
}
