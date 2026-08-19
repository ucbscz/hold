using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class NotificacionRepository
{
    private readonly ApplicationDbContext _db;

    public NotificacionRepository(ApplicationDbContext db) => _db = db;

    public async Task Add(
        string carnet,
        TipoNotificacion type,
        string title,
        string? content = null,
        string? detail = null,
        bool saveChanges = true
    )
    {
        _db.Notificaciones.Add(
            Build(
                new NotificacionDto
                {
                    CarnetUsuario = carnet,
                    Tipo = type.ToString(),
                    Titulo = title,
                    Contenido = content,
                    Detalle = detail,
                }
            )
        );
        if (saveChanges)
            await _db.SaveChangesAsync();
    }

    public async Task AddRange(IReadOnlyCollection<NotificacionDto> notifications)
    {
        if (notifications.Count == 0)
            return;

        _db.Notificaciones.AddRange(notifications.Select(Build));
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetAdminCarnets() =>
        await _db
            .Usuarios.AsNoTracking()
            .Where(u => u.Rol == TipoUsuario.Administrador)
            .Select(u => u.Carnet)
            .ToListAsync();

    public async Task<List<NotificacionDto>> GetByCarnet(string carnet) =>
        await _db
            .Notificaciones.AsNoTracking()
            .Where(n => n.CarnetUsuario == carnet && !n.EstadoEliminado)
            .OrderByDescending(n => n.FechaEnvio)
            .Take(50)
            .Select(n => new NotificacionDto
            {
                Id = n.Id,
                CarnetUsuario = n.CarnetUsuario,
                Tipo = n.Tipo,
                Titulo = n.Titulo,
                Contenido = n.Contenido,
                Detalle = n.Detalle,
                Leido = n.Leido,
                FechaEnvio = n.FechaEnvio,
            })
            .ToListAsync();

    public async Task<bool> MarkAsRead(int id, string carnet) =>
        await _db
            .Notificaciones.Where(n =>
                n.Id == id && n.CarnetUsuario == carnet && !n.EstadoEliminado
            )
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leido, true)) > 0;

    public async Task MarkAllAsRead(string carnet) =>
        await _db
            .Notificaciones.Where(n =>
                n.CarnetUsuario == carnet && !n.Leido && !n.EstadoEliminado
            )
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leido, true));

    private static Notificacion Build(NotificacionDto dto) =>
        new()
        {
            CarnetUsuario = dto.CarnetUsuario ?? string.Empty,
            Tipo = dto.Tipo ?? string.Empty,
            Titulo = dto.Titulo ?? string.Empty,
            Contenido = dto.Contenido,
            Detalle = dto.Detalle,
            Leido = dto.Leido ?? false,
            FechaEnvio = dto.FechaEnvio ?? DateTime.UtcNow,
        };
}
