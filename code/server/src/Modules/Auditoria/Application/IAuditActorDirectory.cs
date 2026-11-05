namespace UCB_Hold.Server.Modules.Auditoria.Application;

public interface IAuditActorDirectory
{
    Task<string?> GetDisplayName(string carnet, CancellationToken cancellationToken = default);
}
