using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Configuracion;

public interface IConfiguracionRepository
{
    Task<ConfiguracionSistema> GetConfiguracion();
    Task Update(ConfiguracionSistema config);
}
