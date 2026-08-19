using Ardalis.Result;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.Notificacion;

public class NotificacionService
{
    private readonly NotificacionRepository _repository;

    public NotificacionService(NotificacionRepository repository) => _repository = repository;

    public async Task Create(
        string carnet,
        TipoNotificacion type,
        string title,
        string? content = null,
        string? detail = null,
        bool saveChanges = true
    ) => await _repository.Add(carnet, type, title, content, detail, saveChanges);

    public async Task CreateMany(IReadOnlyCollection<NotificacionDto> notifications)
    {
        if (notifications.Count == 0)
            return;

        await _repository.AddRange(notifications);
    }

    public async Task CreateForAdmins(TipoNotificacion type, string title, string? content = null)
    {
        var adminCarnets = await _repository.GetAdminCarnets();
        var notifications = adminCarnets
            .Select(carnet => new NotificacionDto
            {
                CarnetUsuario = carnet,
                Tipo = type.ToString(),
                Titulo = title,
                Contenido = content,
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
}
