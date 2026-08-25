using IMT_Reservas.Server.Core.Entities;
using Riok.Mapperly.Abstractions;

namespace IMT_Reservas.Server.Application.Features.Configuracion;

[Mapper]
public partial class ConfiguracionMapper
{
    public partial ConfiguracionDto ToDto(ConfiguracionSistema entity);
    
    [MapperIgnoreTarget(nameof(ConfiguracionSistema.Id))]
    [MapperIgnoreTarget(nameof(ConfiguracionSistema.EstadoEliminado))]
    public partial void UpdateEntity(ConfiguracionDto dto, ConfiguracionSistema entity);
}
