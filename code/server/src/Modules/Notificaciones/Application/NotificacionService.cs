using System.Globalization;
using Ardalis.Result;
using System.Text.Json;
using UCB_Hold.Server.Modules.Notificaciones.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Notificaciones.Application;

public class NotificacionService
{
    public const string SystemEmitter = SystemActor.Nombre;
    public const string DateFormat = "dd/MM/yyyy HH:mm";

    private readonly NotificacionRepository _repository;
    private readonly IDestinatariosAdministradores _admins;

    public NotificacionService(
        NotificacionRepository repository,
        IDestinatariosAdministradores admins
    )
    {
        _repository = repository;
        _admins = admins;
    }

    public async Task Create(
        string carnet,
        TipoNotificacion type,
        string title,
        string? content = null,
        string? detail = null,
        bool saveChanges = true
    ) =>
        await _repository.Add(
            carnet,
            type,
            title,
            content,
            detail ?? BuildEmitterDetail(SystemEmitter),
            saveChanges
        );

    public async Task CreateMany(IReadOnlyCollection<NotificacionDto> notifications)
    {
        if (notifications.Count == 0)
            return;

        foreach (var notification in notifications)
            notification.Detalle ??= BuildEmitterDetail(SystemEmitter);

        await _repository.AddRange(notifications);
    }

    public async Task CreateForAdmins(
        TipoNotificacion type,
        string title,
        string? content = null,
        string emitter = SystemEmitter
    )
    {
        var adminCarnets = await _admins.GetAdminCarnets();
        var notifications = adminCarnets
            .Select(carnet => new NotificacionDto
            {
                CarnetUsuario = carnet,
                Tipo = type.ToString(),
                Titulo = title,
                Contenido = content,
                Detalle = BuildEmitterDetail(emitter),
            })
            .ToList();

        await CreateMany(notifications);
    }

    public async Task<Result<List<NotificacionDto>>> GetByCarnet(string carnet) =>
        Result<List<NotificacionDto>>.Success(await _repository.GetByCarnet(carnet));

    public async Task<Result<object>> MarkAsRead(int id, string carnet)
    {
        var updated = await _repository.MarkAsRead(id, carnet);

        return updated ? Result<object>.Success(null!) : Result<object>.NotFound();
    }

    public async Task<Result<object>> MarkAllAsRead(string carnet)
    {
        await _repository.MarkAllAsRead(carnet);

        return Result<object>.Success(null!);
    }

    public static string BuildEmitterDetail(
        string emitter,
        string? reason = null,
        int? prestamoId = null
    ) =>
        JsonSerializer.Serialize(
            new
            {
                emisor = string.IsNullOrWhiteSpace(emitter) ? SystemEmitter : emitter,
                motivo = reason,
                prestamoId,
                fecha = DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture),
            }
        );
}
