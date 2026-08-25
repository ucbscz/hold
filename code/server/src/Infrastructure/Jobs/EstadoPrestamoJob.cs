using System.Globalization;
using System.Text.Json;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;
using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;
using UsuarioEntity = IMT_Reservas.Server.Core.Entities.Usuario;

namespace IMT_Reservas.Server.Infrastructure.Jobs;

public class EstadoPrestamoJob
{
    private readonly NotificacionService _notifications;
    private readonly AuditLogService _audit;
    private readonly PrestamoRepository _prestamoRepository;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly IAvisoDisponibilidadRepository _availabilityWatches;
    private readonly IConfiguracionRepository _configuracionRepository;

    public EstadoPrestamoJob(
        NotificacionService notifications,
        AuditLogService audit,
        PrestamoRepository prestamoRepository,
        UsuarioRepository usuarioRepository,
        IAvisoDisponibilidadRepository availabilityWatches,
        IConfiguracionRepository configuracionRepository
    )
    {
        _notifications = notifications;
        _audit = audit;
        _prestamoRepository = prestamoRepository;
        _usuarioRepository = usuarioRepository;
        _availabilityWatches = availabilityWatches;
        _configuracionRepository = configuracionRepository;
    }

    public async Task Execute()
    {
        var now = DateTime.UtcNow;
        var config = await _configuracionRepository.GetConfiguracion();

        await ProcessOverdue(now, config.MinutosGraciaAtraso);
        await ProcessExpired(now);
        await ProcessReminders(now, config.TiempoRecordatorioPrevioMinutos);
        await ProcessAvailabilityWatches();
    }

    private async Task ProcessOverdue(DateTime now, int minutosGracia)
    {
        var threshold = now.AddMinutes(-minutosGracia);
        var overdue = await _prestamoRepository.GetOverdueLoans(threshold);

        if (overdue.Count == 0)
            return;

        await _prestamoRepository.MarkAsOverdue(overdue.Select(GetLoanId).ToList());
        var affectedCarnets = overdue.Select(loan => loan.CarnetUsuario ?? string.Empty)
            .Where(carnet => !string.IsNullOrWhiteSpace(carnet))
            .Distinct()
            .ToList();
        var newlyBlockedCarnets = await _usuarioRepository.GetUnblockedCarnets(affectedCarnets);

        await _usuarioRepository.SetBlockedStatus(
            newlyBlockedCarnets,
            true,
            AutomaticBlockReasons.OverdueLoan
        );

        var loanEntries = overdue
                .Select(loan => new AuditEntry(
                    AuditAccion.AtrasadoAutomatico,
                    nameof(PrestamoEntity),
                    GetLoanIdText(loan),
                    BuildLoanDetail(
                        loan,
                        "Bloqueo automático por préstamo vencido.",
                        loan.FechaDevolucionEsperada
                    )
                ));
        var userEntries = newlyBlockedCarnets
            .Select(carnet => new AuditEntry(
                AuditAccion.Bloquear,
                nameof(UsuarioEntity),
                carnet,
                AutomaticBlockReasons.OverdueLoan
            ));

        await _audit.LogMany(loanEntries.Concat(userEntries).ToList());

        await _notifications.CreateMany(
            overdue
                .Select(loan => new NotificacionDto
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
                })
                .ToList()
        );

        await _notifications.CreateForAdmins(
            TipoNotificacion.AdminPrestamoAtrasado,
            "Préstamo atrasado",
            $"Hay {overdue.Count} préstamo(s) atrasado(s) sin devolver."
        );
    }

    private async Task ProcessExpired(DateTime now)
    {
        var expired = await _prestamoRepository.GetExpiredPendingLoans(now);

        if (expired.Count == 0)
            return;

        await _prestamoRepository.MarkAsRejected(expired.Select(GetLoanId).ToList());

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

        await _notifications.CreateMany(
            expired
                .Select(loan => new NotificacionDto
                {
                    CarnetUsuario = loan.CarnetUsuario,
                    Tipo = nameof(TipoNotificacion.PrestamoRechazado),
                    Titulo = "Préstamo rechazado",
                    Contenido =
                        "Tu solicitud fue rechazada automáticamente por exceder la fecha de inicio.",
                })
                .ToList()
        );
    }

    private async Task ProcessReminders(DateTime now, int reminderMinutes)
    {
        var dueSoon = await _prestamoRepository.GetLoansDueForReminder(
            now,
            now.AddMinutes(reminderMinutes)
        );

        if (dueSoon.Count == 0)
            return;

        await _prestamoRepository.MarkReminderSent(dueSoon.Select(GetLoanId).ToList());

        await _notifications.CreateMany(
            dueSoon
                .Select(loan => new NotificacionDto
                {
                    CarnetUsuario = loan.CarnetUsuario,
                    Tipo = nameof(TipoNotificacion.RecordatorioDevolucion),
                    Titulo = "Recordatorio de devolución",
                    Contenido = "Tu préstamo vence en menos de 30 minutos. No olvides devolver los equipos a tiempo.",
                })
                .ToList()
        );
    }

    private async Task ProcessAvailabilityWatches()
    {
        var pending = await _availabilityWatches.GetPending();

        if (pending.Count == 0)
            return;

        var config = await _configuracionRepository.GetConfiguracion();

        var notified = new List<int>();
        var notifications = new List<NotificacionDto>();

        foreach (var watch in pending)
        {
            var date = watch.Fecha;

            if (!HorarioReserva.EsValido(date, date.AddMinutes(config.TiempoMinimoReservaMinutos), config.HorarioInicioMinutos, config.HorarioFinMinutos))
                continue;

            if (await _prestamoRepository.HasAvailableEquipo(
                watch.IdGrupoEquipo,
                date,
                date.AddMinutes(config.TiempoMinimoReservaMinutos)
            ))
            {
                notifications.Add(
                    new NotificacionDto
                    {
                        CarnetUsuario = watch.CarnetUsuario,
                        Tipo = nameof(TipoNotificacion.DisponibilidadLiberada),
                        Titulo = "Disponibilidad liberada",
                        Contenido =
                            $"Un equipo que esperabas está disponible el {watch.Fecha:dd/MM/yyyy HH:mm}.",
                    }
                );
                notified.Add(watch.Id);
            }
        }

        if (notified.Count == 0)
            return;

        await _notifications.CreateMany(notifications);
        await _availabilityWatches.MarkAsNotified(notified);
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
                emisor = "Sistema",
                carnet = loan.CarnetUsuario,
                producto = loan.NombreGrupoEquipo,
                motivo,
                fecha = fechaRelacionada?.ToString("dd/MM/yyyy HH:mm"),
            }
        );
}
