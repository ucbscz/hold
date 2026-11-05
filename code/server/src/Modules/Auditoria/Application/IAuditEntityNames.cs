namespace UCB_Hold.Server.Modules.Auditoria.Application;

public interface IAuditEntityNames
{
    Task<Dictionary<string, string>?> FindNames(string entity, IReadOnlyCollection<string> ids);
}
