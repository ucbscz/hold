using System.Globalization;
using System.Text.Json;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;

namespace IMT_Reservas.Server.Infrastructure.Jobs;

public class EstadoPrestamoJob
{
    private readonly NotificacionService _notifications;
    private readonly AuditLogService _audit;
    private readonly PrestamoRepository _prestamoRepository;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly AvisoDisponibilidadRepository _availabilityWatches;

    public EstadoPrestamoJob(
        NotificacionService notifications,
        AuditLogService audit,
        PrestamoRepository prestamoRepository,
        UsuarioRepository usuarioRepository,
        AvisoDisponibilidadRepository availabilityWatches
    )
    {
        _notifications = notifications;
        _audit = audit;
        _prestamoRepository = prestamoRepository;
        _usuarioRepository = usuarioRepository;
        _availabilityWatches = availabilityWatches;
    }

    public async Task Execute()
    {
        var now = DateTime.UtcNow;

        await ProcessOverdue(now);
        await ProcessExpired(now);
        await ProcessReminders(now);
        await ProcessAvailabilityWatches();
    }

    private async Task ProcessOverdue(DateTime now)
    {
        var overdue = await _prestamoRepository.GetOverdueLoans(now);

        if (overdue.Count == 0)
            return;

        await _prestamoRepository.MarkAsOverdue(overdue.Select(GetLoanId).ToList());
        await _usuarioRepository.SetBlockedStatus(
            overdue.Select(loan => loan.CarnetUsuario ?? string.Empty)
                .Where(carnet => !string.IsNullOrWhiteSpace(carnet))
                .Distinct()
                .ToList(),
            true,
            "Cuenta bloqueada automáticamente por préstamo atrasado."
        );

        await _audit.LogMany(
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
                .ToList()
        );

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

    private async Task ProcessReminders(DateTime now)
    {
        var dueSoon = await _prestamoRepository.GetLoansDueForReminder(
            now,
            now.AddMinutes(30)
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

        var notified = new List<int>();
        var notifications = new List<NotificacionDto>();

        foreach (var watch in pending)
        {
            var date = watch.Fecha;

            if (await _prestamoRepository.HasAvailableEquipo(
                watch.IdGrupoEquipo,
                date,
                date.AddMinutes(30)
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
                origen = "Sistema de préstamos",
                usuario = $"{loan.NombreUsuario ?? "Usuario"} {loan.ApellidoPaternoUsuario ?? string.Empty}".Trim(),
                carnet = loan.CarnetUsuario,
                producto = loan.NombreGrupoEquipo,
                motivo,
                fecha = fechaRelacionada?.ToString("dd/MM/yyyy HH:mm"),
            }
        );
}
