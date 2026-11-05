namespace UCB_Hold.Server.Modules.Notificaciones.Application;

public interface IDestinatariosAdministradores
{
    Task<List<string>> GetAdminCarnets(CancellationToken cancellationToken = default);
}
