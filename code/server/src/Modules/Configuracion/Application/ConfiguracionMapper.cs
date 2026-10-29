using Riok.Mapperly.Abstractions;
using IMT_Reservas.Server.Modules.Configuracion.Domain;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Configuracion.Application;

[Mapper]
public partial class ConfiguracionMapper : IMapper<ConfiguracionSistema, ConfiguracionDto>
{
    public partial ConfiguracionDto ToDto(ConfiguracionSistema entity);

    public partial ConfiguracionSistema ToEntity(ConfiguracionDto dto);

    public IQueryable<ConfiguracionDto> ProjectTo(IQueryable<ConfiguracionSistema> source)
        => source.Select(e => ToDto(e));

    [MapperIgnoreTarget(nameof(ConfiguracionSistema.Id))]
    [MapperIgnoreTarget(nameof(ConfiguracionSistema.EstadoEliminado))]
    public partial void UpdateEntity(ConfiguracionDto dto, ConfiguracionSistema entity);

    private static HorarioAtencionDto MapSchedule(HorarioAtencion source) => new()
    {
        DiaSemana = source.DiaSemana,
        Fecha = source.Fecha,
        Abierto = source.Abierto,
        InicioMinutos = source.InicioMinutos,
        FinMinutos = source.FinMinutos,
    };

    private static HorarioAtencion MapSchedule(HorarioAtencionDto source) => new()
    {
        DiaSemana = source.DiaSemana,
        Fecha = source.Fecha,
        Abierto = source.Abierto,
        InicioMinutos = source.InicioMinutos,
        FinMinutos = source.FinMinutos,
    };
}
