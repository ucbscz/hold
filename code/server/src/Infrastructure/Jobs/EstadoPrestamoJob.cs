using System.Globalization;
using System.Text.Json;
using IMT_Reservas.Server.Application.Features.AuditLog;
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
    private readonly PrestamoEstadoRepository _prestamoRepository;
    private readonly PrestamoDisponibilidadRepository _availability;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly UsuarioReadRepository _usuarioQueries;
    private readonly AvisoDisponibilidadRepository _availabilityWatches;
    private readonly ConfiguracionRepository _configuracionRepository;

    public EstadoPrestamoJob(
        NotificacionService notifications,
        AuditLogService audit,
        PrestamoEstadoRepository prestamoRepository,
        PrestamoDisponibilidadRepository availability,
        UsuarioRepository usuarioRepository,
        UsuarioReadRepository usuarioQueries,
        AvisoDisponibilidadRepository availabilityWatches,
        ConfiguracionRepository configuracionRepository
    )
    {
        _notifications = notifications;
        _audit = audit;
        _prestamoRepository = prestamoRepository;
        _availability = availability;
        _usuarioRepository = usuarioRepository;
        _usuarioQueries = usuarioQueries;
        _availabilityWatches = availabilityWatches;
        _configuracionRepository = configuracionRepository;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var config = await _configuracionRepository.GetConfiguracion();

        await ProcessOverdue(now, config.MinutosGraciaAtraso, cancellationToken);
        await ProcessExpired(now, cancellationToken);
        await ProcessReminders(
            now,
            config.TiempoRecordatorioPrevioMinutos,
            cancellationToken
        );
        await ProcessAvailabilityWatches(cancellationToken);
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

        await _notifications.CreateMany(
            expired
                .Select(loan => new NotificacionDto
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
                })
                .ToList()
        );
    }

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
                    Contenido = "Tu préstamo vence en menos de 30 minutos. No olvides devolver los equipos a tiempo.",
                })
                .ToList()
        );
    }

    private async Task ProcessAvailabilityWatches(CancellationToken cancellationToken)
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

            if (!HorarioReserva.EsValido(date, date.AddMinutes(config.TiempoMinimoReservaMinutos), config))
                continue;

            if (await _availability.HasAvailableEquipo(
                watch.IdGrupoEquipo,
                date,
                date.AddMinutes(config.TiempoMinimoReservaMinutos),
                cancellationToken
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
                        Detalle = JsonSerializer.Serialize(new
                        {
                            emisor = "Sistema",
                            grupoEquipoId = watch.IdGrupoEquipo,
                            fecha = watch.Fecha.ToString("dd/MM/yyyy HH:mm"),
                        }),
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
                prestamoId = loan.Id,
                carnet = loan.CarnetUsuario,
                producto = loan.NombreGrupoEquipo,
                motivo,
                fecha = fechaRelacionada?.ToString("dd/MM/yyyy HH:mm"),
            }
        );
}
