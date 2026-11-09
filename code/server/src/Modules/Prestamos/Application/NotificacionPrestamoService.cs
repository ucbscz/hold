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
        var message = StatusMessage(estado, observacion, retorno);

        if (message == null)
            return;

        await _notifications.Create(
            carnet,
            message.Tipo,
            message.Titulo,
            message.Contenido,
            NotificacionService.BuildEmitterDetail(emitter, message.Detalle, prestamoId)
        );
    }

    private static StatusMessageData? StatusMessage(
        EstadoPrestamo estado,
        string? observacion,
        RetornoEquipos? retorno
    )
    {
        var sinObservacion = string.IsNullOrWhiteSpace(observacion);

        return estado switch
        {
            EstadoPrestamo.Aprobado => new StatusMessageData(
                TipoNotificacion.PrestamoAprobado,
                "Préstamo aprobado",
                sinObservacion
                    ? "Tu solicitud de préstamo fue aprobada. Ya puedes revisar los detalles de recogida."
                    : $"Tu solicitud de préstamo fue aprobada. Detalle: {observacion!.Trim()}",
                sinObservacion ? "Solicitud aprobada." : observacion!.Trim()
            ),
            EstadoPrestamo.Rechazado => new StatusMessageData(
                TipoNotificacion.PrestamoRechazado,
                "Préstamo rechazado",
                sinObservacion
                    ? "Tu solicitud de préstamo fue rechazada."
                    : $"Tu solicitud de préstamo fue rechazada: {observacion}",
                observacion ?? "Solicitud rechazada."
            ),
            EstadoPrestamo.Finalizado when retorno?.HasDamagedEquipment == true => new StatusMessageData(
                TipoNotificacion.EquipoObservacion,
                "Equipo marcado como inoperativo",
                retorno.UserMessage,
                observacion ?? "Equipo devuelto con observaciones."
            ),
            _ => null,
        };
    }

    private sealed record StatusMessageData(
        TipoNotificacion Tipo,
        string Titulo,
        string? Contenido,
        string Detalle
    );

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
