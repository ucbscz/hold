using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Core.Entities;
using Riok.Mapperly.Abstractions;

namespace IMT_Reservas.Server.Application.Features.Configuracion;

[Mapper]
public partial class ConfiguracionMapper : IUpdateMapper<ConfiguracionSistema, ConfiguracionDto>
{
    public partial ConfiguracionDto ToDto(ConfiguracionSistema entity);

    public partial ConfiguracionSistema ToEntity(ConfiguracionDto dto);

    public IQueryable<ConfiguracionDto> ProjectTo(IQueryable<ConfiguracionSistema> source)
        => source.Select(e => ToDto(e));

    [MapperIgnoreTarget(nameof(ConfiguracionSistema.Id))]
    [MapperIgnoreTarget(nameof(ConfiguracionSistema.EstadoEliminado))]
    public partial void UpdateEntity(ConfiguracionDto dto, ConfiguracionSistema entity);
}
