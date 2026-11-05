using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Configuracion.Domain;

namespace UCB_Hold.Server.Modules.Configuracion.Infrastructure;

public sealed class ConfiguracionAuditNames : IAuditEntityNames
{
    private const string Name = "Configuración del sistema";

    public Task<Dictionary<string, string>?> FindNames(
        string entity,
        IReadOnlyCollection<string> ids
    ) =>
        Task.FromResult(
            entity == nameof(ConfiguracionSistema)
                ? AuditEntityLabel.ToNames(
                    AuditEntityLabel.NumericIds(ids).Select(id => new AuditEntityLabel(id, Name))
                )
                : null
        );
}
