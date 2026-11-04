using UCB_Hold.Server.Modules.Notificaciones.Application;
using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public sealed class NotificacionPrestamoService
{
    private readonly NotificacionService _notifications;

    public NotificacionPrestamoService(NotificacionService notifications) =>
        _notifications = notifications;

    public async Task NotifyStatusChange(
        int prestamoId,
        string carnet,
        EstadoPrestamo estado,
        string? observacion,
        RetornoEquipos? retorno,
        string emitter
    )
    {
        switch (estado)
        {
            case EstadoPrestamo.Aprobado:
                var approvalDetail = string.IsNullOrWhiteSpace(observacion)
                    ? "Solicitud aprobada."
                    : observacion.Trim();
                await _notifications.Create(
                    carnet,
                    TipoNotificacion.PrestamoAprobado,
                    "Préstamo aprobado",
                    string.IsNullOrWhiteSpace(observacion)
                        ? "Tu solicitud de préstamo fue aprobada. Ya puedes revisar los detalles de recogida."
                        : $"Tu solicitud de préstamo fue aprobada. Detalle: {approvalDetail}",
                    NotificacionService.BuildEmitterDetail(emitter, approvalDetail, prestamoId)
                );
                break;
            case EstadoPrestamo.Rechazado:
                await _notifications.Create(
                    carnet,
                    TipoNotificacion.PrestamoRechazado,
                    "Préstamo rechazado",
                    string.IsNullOrWhiteSpace(observacion)
                        ? "Tu solicitud de préstamo fue rechazada."
                        : $"Tu solicitud de préstamo fue rechazada: {observacion}",
                    NotificacionService.BuildEmitterDetail(
                        emitter,
                        observacion ?? "Solicitud rechazada.",
                        prestamoId
                    )
                );
                break;
            case EstadoPrestamo.Finalizado when retorno?.HasDamagedEquipment == true:
                await _notifications.Create(
                    carnet,
                    TipoNotificacion.EquipoObservacion,
                    "Equipo marcado como inoperativo",
                    retorno.UserMessage,
                    NotificacionService.BuildEmitterDetail(
                        emitter,
                        observacion ?? "Equipo devuelto con observaciones.",
                        prestamoId
                    )
                );
                break;
        }
    }

    public Task NotifyAutomaticUnblock(string carnet) =>
        _notifications.Create(
            carnet,
            TipoNotificacion.UsuarioDesbloqueado,
            "Cuenta desbloqueada para reservas",
            "Tu cuenta fue desbloqueada porque ya no tienes préstamos atrasados.",
            NotificacionService.BuildEmitterDetail(
                NotificacionService.SystemEmitter,
                "Todos los préstamos atrasados fueron regularizados."
            )
        );
}
