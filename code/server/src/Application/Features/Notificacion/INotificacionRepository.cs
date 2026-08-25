namespace IMT_Reservas.Server.Application.Features.Notificacion;

public interface INotificacionRepository
{
    Task Add(
        string carnet,
        TipoNotificacion type,
        string title,
        string? content = null,
        string? detail = null,
        bool saveChanges = true
    );

    Task AddRange(IReadOnlyCollection<NotificacionDto> notifications);
    Task<List<string>> GetAdminCarnets();
    Task<List<NotificacionDto>> GetByCarnet(string carnet);
    Task<bool> MarkAsRead(int id, string carnet);
    Task MarkAllAsRead(string carnet);
}
