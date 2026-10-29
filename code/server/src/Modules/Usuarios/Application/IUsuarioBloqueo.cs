namespace UCB_Hold.Server.Modules.Usuarios.Application;

public interface IUsuarioBloqueo
{
    Task SetBlockedStatus(
        IReadOnlyCollection<string> carnets,
        bool isBlocked,
        string? reason,
        CancellationToken cancellationToken = default
    );
}
