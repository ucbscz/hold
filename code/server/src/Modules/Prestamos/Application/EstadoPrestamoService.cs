using System.Globalization;
using System.Text.Json;
using PrestamoEntity = UCB_Hold.Server.Modules.Prestamos.Domain.Prestamo;
using UsuarioEntity = UCB_Hold.Server.Modules.Usuarios.Domain.Usuario;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Configuracion.Application;
using UCB_Hold.Server.Modules.Notificaciones.Application;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;
using UCB_Hold.Server.Modules.Usuarios.Application;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public class EstadoPrestamoService
{
    private readonly NotificacionService _notifications;
    private readonly AuditLogService _audit;
    private readonly PrestamoEstadoRepository _prestamoRepository;
    private readonly IUsuarioBloqueo _usuarioRepository;
    private readonly IUsuarioConsultas _usuarioQueries;
    private readonly AvisoDisponibilidadService _availabilityWatches;
    private readonly IConfiguracionProvider _configuracionRepository;

    public EstadoPrestamoService(
        NotificacionService notifications,
        AuditLogService audit,
        PrestamoEstadoRepository prestamoRepository,
        IUsuarioBloqueo usuarioRepository,
        IUsuarioConsultas usuarioQueries,
        AvisoDisponibilidadService availabilityWatches,
        IConfiguracionProvider configuracionRepository
    )
    {
        _notifications = notifications;
        _audit = audit;
        _prestamoRepository = prestamoRepository;
        _usuarioRepository = usuarioRepository;
        _usuarioQueries = usuarioQueries;
        _availabilityWatches = availabilityWatches;
        _configuracionRepository = configuracionRepository;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var config = await _configuracionRepository.GetConfiguracion(cancellationToken);

        await ProcessOverdue(now, config.MinutosGraciaAtraso, cancellationToken);
        await ProcessExpired(now, cancellationToken);
        await ProcessReminders(
            now,
            config.TiempoRecordatorioPrevioMinutos,
            cancellationToken
        );
        await _availabilityWatches.NotifyAvailable(config, cancellationToken);
    }

    private async Task ProcessOverdue(
        DateTime now,
        int minutosGracia,
        CancellationToken cancellationToken
    )
    {
        var threshold = now.AddMinutes(-minutosGracia);
        var overdue = await _prestamoRepository.GetOverdueLoans(
            threshold,
            cancellationToken
        );

        if (overdue.Count == 0)
            return;

        await _prestamoRepository.MarkAsOverdue(
            overdue.Select(GetLoanId).ToList(),
            cancellationToken
        );
        var newlyBlockedCarnets = await BlockOverdueUsers(overdue, cancellationToken);

        await _audit.LogMany(OverdueAuditEntries(overdue, newlyBlockedCarnets));
        await _notifications.CreateMany(overdue.Select(OverdueNotification).ToList());
        await _notifications.CreateForAdmins(
            TipoNotificacion.AdminPrestamoAtrasado,
            "Préstamo atrasado",
            $"Hay {overdue.Count} préstamo(s) atrasado(s) sin devolver."
        );
    }

    private async Task<List<string>> BlockOverdueUsers(
        List<PrestamoDto> overdue,
        CancellationToken cancellationToken
    )
    {
        var affectedCarnets = overdue.Select(loan => loan.CarnetUsuario ?? string.Empty)
            .Where(carnet => !string.IsNullOrWhiteSpace(carnet))
            .Distinct()
            .ToList();
        var newlyBlockedCarnets = await _usuarioQueries.GetUnblockedCarnets(
            affectedCarnets,
            cancellationToken
        );

        await _usuarioRepository.SetBlockedStatus(
            newlyBlockedCarnets,
            true,
            AutomaticBlockReasons.OverdueLoan,
            cancellationToken
        );

        return newlyBlockedCarnets;
    }

    private static List<AuditEntry> OverdueAuditEntries(
        List<PrestamoDto> overdue,
        List<string> newlyBlockedCarnets
    ) =>
        overdue
            .Select(loan => new AuditEntry(
                AuditAccion.AtrasadoAutomatico,
                nameof(PrestamoEntity),
                GetLoanIdText(loan),
                BuildLoanDetail(
                    loan,
                    "Bloqueo automático por préstamo vencido.",
                    loan.FechaDevolucionEsperada
                )
            ))
            .Concat(newlyBlockedCarnets.Select(carnet => new AuditEntry(
                AuditAccion.Bloquear,
                nameof(UsuarioEntity),
                carnet,
                AutomaticBlockReasons.OverdueLoan
            )))
            .ToList();

    private static NotificacionDto OverdueNotification(PrestamoDto loan) =>
        new()
        {
            CarnetUsuario = loan.CarnetUsuario,
            Tipo = nameof(TipoNotificacion.PrestamoAtrasado),
            Titulo = "Cuenta bloqueada por atraso",
            Contenido =
                "Tu préstamo está atrasado. Tu cuenta queda bloqueada para nuevas reservas hasta que devuelvas los equipos. Si necesitas ayuda, contacta con un administrador.",
            Detalle = BuildLoanDetail(
                loan,
                "El préstamo no fue devuelto dentro del horario acordado.",
                loan.FechaDevolucionEsperada
            ),
        };

    private async Task ProcessExpired(DateTime now, CancellationToken cancellationToken)
    {
        var expired = await _prestamoRepository.GetExpiredPendingLoans(
            now,
            cancellationToken
        );

        if (expired.Count == 0)
            return;

        await _prestamoRepository.MarkAsRejected(
            expired.Select(GetLoanId).ToList(),
            cancellationToken
        );

        await _audit.LogMany(
            expired
                .Select(loan => new AuditEntry(
                    AuditAccion.Rechazar,
                    nameof(PrestamoEntity),
                    GetLoanIdText(loan),
                    "Auto-rechazado por exceder fecha de inicio"
                ))
                .ToList()
        );

        await _notifications.CreateMany(expired.Select(ExpiredNotification).ToList());
    }

    private static NotificacionDto ExpiredNotification(PrestamoDto loan) =>
        new()
        {
            CarnetUsuario = loan.CarnetUsuario,
            Tipo = nameof(TipoNotificacion.PrestamoRechazado),
            Titulo = "Préstamo rechazado",
            Contenido =
                "Tu solicitud fue rechazada automáticamente por exceder la fecha de inicio.",
            Detalle = BuildLoanDetail(
                loan,
                "La solicitud no fue recogida antes de la fecha de inicio acordada.",
                loan.FechaPrestamoEsperada
            ),
        };

    private async Task ProcessReminders(
        DateTime now,
        int reminderMinutes,
        CancellationToken cancellationToken
    )
    {
        var dueSoon = await _prestamoRepository.GetLoansDueForReminder(
            now,
            now.AddMinutes(reminderMinutes),
            cancellationToken
        );

        if (dueSoon.Count == 0)
            return;

        await _prestamoRepository.MarkReminderSent(
            dueSoon.Select(GetLoanId).ToList(),
            cancellationToken
        );

        await _notifications.CreateMany(
            dueSoon
                .Select(loan => new NotificacionDto
                {
                    CarnetUsuario = loan.CarnetUsuario,
                    Tipo = nameof(TipoNotificacion.RecordatorioDevolucion),
                    Titulo = "Recordatorio de devolución",
                    Contenido =
                        $"Tu préstamo vence en menos de {reminderMinutes} minutos. No olvides devolver los equipos a tiempo.",
                })
                .ToList()
        );
    }

    private static int GetLoanId(PrestamoDto loan) => loan.Id.GetValueOrDefault();

    private static string GetLoanIdText(PrestamoDto loan) =>
        GetLoanId(loan).ToString(CultureInfo.InvariantCulture);

    private static string BuildLoanDetail(
        PrestamoDto loan,
        string motivo,
        DateTime? fechaRelacionada
    ) =>
        JsonSerializer.Serialize(
            new
            {
                emisor = NotificacionService.SystemEmitter,
                prestamoId = loan.Id,
                carnet = loan.CarnetUsuario,
                producto = loan.NombreGrupoEquipo,
                motivo,
                fecha = fechaRelacionada?.ToString(NotificacionService.DateFormat, CultureInfo.InvariantCulture),
            }
        );
}
