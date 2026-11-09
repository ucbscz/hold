namespace UCB_Hold.Server.Shared.Kernel;

public interface IAuditTrail
{
    Task Created<T>(string entity, string? id, T current);

    Task Updated<T>(string entity, string id, T? previous, T current);

    Task Deleted<T>(string entity, string id, T? previous);
}
