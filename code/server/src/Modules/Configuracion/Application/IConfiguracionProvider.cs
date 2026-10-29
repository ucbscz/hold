using UCB_Hold.Server.Modules.Configuracion.Domain;

namespace UCB_Hold.Server.Modules.Configuracion.Application;

public interface IConfiguracionProvider
{
    Task<ConfiguracionSistema> GetConfiguracion(CancellationToken cancellationToken = default);
}
