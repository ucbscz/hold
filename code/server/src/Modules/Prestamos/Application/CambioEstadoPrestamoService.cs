using System.Globalization;
using Ardalis.Result;
using PrestamoEntity = UCB_Hold.Server.Modules.Prestamos.Domain.Prestamo;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Configuracion.Application;
using UCB_Hold.Server.Modules.Prestamos.Application.State;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public sealed class CambioEstadoPrestamoService
{
    private readonly PrestamoRepository _repository;
    private readonly PrestamoReadRepository _queries;
    private readonly PrestamoDisponibilidadRepository _availability;
    private readonly AuditLogService _audit;
    private readonly IUsuarioBloqueo _usuarioBloqueo;
    private readonly AvisoDisponibilidadService _availabilityWatches;
    private readonly IConfiguracionProvider _configuracion;
    private readonly RetornoEquiposService _retornos;
    private readonly NotificacionPrestamoService _notificaciones;

    public CambioEstadoPrestamoService(
        PrestamoRepository repository,
        PrestamoReadRepository queries,
        PrestamoDisponibilidadRepository availability,
        AuditLogService audit,
        IUsuarioBloqueo usuarioBloqueo,
        AvisoDisponibilidadService availabilityWatches,
        IConfiguracionProvider configuracion,
        RetornoEquiposService retornos,
        NotificacionPrestamoService notificaciones
    )
    {
        _repository = repository;
        _queries = queries;
        _availability = availability;
        _audit = audit;
        _usuarioBloqueo = usuarioBloqueo;
        _availabilityWatches = availabilityWatches;
        _configuracion = configuracion;
        _retornos = retornos;
        _notificaciones = notificaciones;
    }

    public async Task<Result<PrestamoDto>> ChangeStatus(
        int id,
        PrestamoDto request,
        string carnet,
        bool puedeGestionar,
        CancellationToken cancellationToken
    )
    {
        if (puedeGestionar)
            return await UpdateStatus(
                id,
                request.EstadoPrestamo ?? string.Empty,
                request.Observacion,
                request,
                carnet,
                cancellationToken
            );

        return PrestamoState.Parse(request.EstadoPrestamo) == EstadoPrestamo.Cancelado
            ? await CancelForUser(id, carnet, cancellationToken)
            : Result<PrestamoDto>.Forbidden();
    }

    public async Task<Result<PrestamoDto>> CancelForUser(int id, string carnet, CancellationToken cancellationToken)
    {
        var loan = await _repository.FindById(id, cancellationToken);
        if (loan == null || loan.Carnet != carnet) return Result<PrestamoDto>.NotFound();
        if (loan.EstadoPrestamo != EstadoPrestamo.Pendiente && loan.EstadoPrestamo != EstadoPrestamo.Aprobado)
            return Result<PrestamoDto>.Error("Solo puedes cancelar préstamos pendientes o aprobados");
        return await UpdateStatus(id, PrestamoState.ToText(EstadoPrestamo.Cancelado), actorCarnet: carnet, cancellationToken: cancellationToken);
    }

    public async Task<Result<PrestamoDto>> UpdateStatus(
        int id,
        string newStatus,
        string? observacion = null,
        PrestamoDto? body = null,
        string? actorCarnet = null,
        CancellationToken cancellationToken = default
    )
    {
        var loan = await _repository.FindById(id, cancellationToken);

        if (loan == null)
            return Result<PrestamoDto>.NotFound();

        var parsedState = PrestamoState.Parse(newStatus);

        if (!parsedState.HasValue)
            return Result<PrestamoDto>.Error($"Estado '{newStatus}' no reconocido");

        var state = parsedState.Value;
        var rejection = RejectTransition(loan, state, newStatus, observacion);
        if (rejection != null)
            return rejection;

        var actor = string.IsNullOrWhiteSpace(actorCarnet)
            ? SystemActor.Nombre
            : await _queries.GetUsuarioDisplayName(actorCarnet, cancellationToken);

        if (
            state == EstadoPrestamo.Aprobado
            && !await _availability.AssignEquiposOnApproval(id, actor, cancellationToken)
        )
            return Result<PrestamoDto>.Error(
                "No se puede aprobar: no hay equipos disponibles para uno o más grupos en las fechas solicitadas"
            );

        var previous = AuditSnapshot(loan);
        ApplyState(loan, state, actor, observacion);
        await _repository.UpdateTracked(loan, cancellationToken);

        RetornoEquipos? retorno = null;
        string? auditDetail;

        if (state == EstadoPrestamo.Finalizado)
        {
            retorno = await _retornos.Apply(id, observacion, body, previous.EstadoPrestamo, cancellationToken);
            auditDetail = retorno.AuditDetail;
        }
        else
            auditDetail = AuditChangeDetail.Build(previous, AuditSnapshot(loan));

        await _audit.Log(
            ToAuditAction(state),
            typeof(PrestamoEntity).Name,
            id.ToString(CultureInfo.InvariantCulture),
            auditDetail
        );

        await _notificaciones.NotifyStatusChange(
            id,
            loan.Carnet!,
            state,
            observacion,
            retorno,
            actor ?? SystemActor.Nombre
        );

        await AfterTransition(loan.Carnet!, state, cancellationToken);

        return await _queries.Get(id, cancellationToken);
    }

    private static Result<PrestamoDto>? RejectTransition(
        PrestamoEntity loan,
        EstadoPrestamo state,
        string newStatus,
        string? observacion
    )
    {
        if (state == EstadoPrestamo.Rechazado && string.IsNullOrWhiteSpace(observacion))
            return Result<PrestamoDto>.Error("Indica el motivo del rechazo");

        if (observacion?.Length > 1024)
            return Result<PrestamoDto>.Error("La observación no puede superar 1024 caracteres");

        if (!PrestamoState.CanTransition(loan.EstadoPrestamo, state))
            return Result<PrestamoDto>.Error(
                $"Transición '{PrestamoState.ToText(loan.EstadoPrestamo)}' → '{newStatus}' no permitida"
            );

        if (state == EstadoPrestamo.Aprobado && loan.FechaPrestamoEsperada <= DateTime.UtcNow)
            return Result<PrestamoDto>.Error(
                "No se puede aprobar una reserva cuyo horario de inicio ya venció"
            );

        return null;
    }

    private static PrestamoAuditSnapshot AuditSnapshot(PrestamoEntity loan) =>
        new(loan.EstadoPrestamo, loan.Observacion, loan.MotivoRechazo, loan.AutorizadoPor, loan.EntregadoPor);

    private static void ApplyState(
        PrestamoEntity loan,
        EstadoPrestamo state,
        string? actor,
        string? observacion
    )
    {
        loan.EstadoPrestamo = state;

        if (state == EstadoPrestamo.Aprobado)
            loan.AutorizadoPor = actor;
        if (state == EstadoPrestamo.Activo)
            loan.EntregadoPor = actor;
        if (state == EstadoPrestamo.Rechazado)
            loan.MotivoRechazo = observacion!.Trim();
        if (observacion != null)
            loan.Observacion = observacion;
        if (state == EstadoPrestamo.Finalizado)
            loan.FechaDevolucion = DateTime.UtcNow;
    }

    private async Task AfterTransition(
        string carnet,
        EstadoPrestamo state,
        CancellationToken cancellationToken
    )
    {
        if (state == EstadoPrestamo.Finalizado)
            await UnblockIfNoOverdueLoans(carnet, cancellationToken);

        if (ReleasesAvailability(state))
            await _availabilityWatches.NotifyAvailable(
                await _configuracion.GetConfiguracion(cancellationToken),
                cancellationToken
            );
    }

    private sealed record PrestamoAuditSnapshot(
        EstadoPrestamo EstadoPrestamo,
        string? Observacion,
        string? MotivoRechazo,
        string? AutorizadoPor,
        string? EntregadoPor
    );

    private static AuditAccion ToAuditAction(EstadoPrestamo estado) =>
        estado switch
        {
            EstadoPrestamo.Aprobado => AuditAccion.Aprobar,
            EstadoPrestamo.Rechazado => AuditAccion.Rechazar,
            EstadoPrestamo.Activo => AuditAccion.Recoger,
            EstadoPrestamo.Finalizado => AuditAccion.Devolver,
            EstadoPrestamo.Cancelado => AuditAccion.Cancelar,
            EstadoPrestamo.Atrasado => AuditAccion.AtrasadoAutomatico,
            _ => AuditAccion.Editar,
        };

    private static bool ReleasesAvailability(EstadoPrestamo estado) =>
        estado is EstadoPrestamo.Finalizado or EstadoPrestamo.Cancelado or EstadoPrestamo.Rechazado;

    private async Task UnblockIfNoOverdueLoans(
        string carnet,
        CancellationToken cancellationToken
    )
    {
        if (await _queries.HasAtrasadoPrestamo(carnet, cancellationToken))
            return;

        var blockReason = await _queries.GetBlockReason(carnet, cancellationToken);

        if (!string.Equals(
            blockReason,
            AutomaticBlockReasons.OverdueLoan,
            StringComparison.Ordinal
        ))
            return;

        await _usuarioBloqueo.SetBlockedStatus([carnet], false, null, cancellationToken);
        await _audit.LogAsSystem(
            AuditAccion.Desbloquear,
            "Usuario",
            carnet,
            AuditChangeDetail.Build(
                new { Bloqueado = true, MotivoBloqueo = (string?)"Préstamos atrasados" },
                new { Bloqueado = false, MotivoBloqueo = (string?)null }
            )
        );
        await _notificaciones.NotifyAutomaticUnblock(carnet);
    }
}
