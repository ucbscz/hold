namespace UCB_Hold.Server.Modules.Usuarios.Application;

public interface IUsuarioConsultas
{
    Task<string?> GetDisplayName(string carnet, CancellationToken cancellationToken = default);

    Task<List<string>> GetUnblockedCarnets(
        IReadOnlyCollection<string> carnets,
        CancellationToken cancellationToken = default
    );
}
