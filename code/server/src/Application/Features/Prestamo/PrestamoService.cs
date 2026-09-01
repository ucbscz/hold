using System.Text.Json;
using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Features.Prestamo.State;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;

namespace IMT_Reservas.Server.Application.Features.Prestamo;

public class PrestamoService : Service<PrestamoEntity, PrestamoRepository, PrestamoDto>
{
    private readonly NotificacionService _notifications;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly AvisoDisponibilidadRepository _availabilityWatches;
    private readonly ConfiguracionRepository _configuracionRepository;
    private readonly PrestamoReadRepository _queries;
    private readonly PrestamoEstadoRepository _states;
    private readonly PrestamoDisponibilidadRepository _availability;

    public PrestamoService(
        PrestamoRepository repository,
        PrestamoMapper mapper,
        IValidator<PrestamoDto> validator,
        AuditLogService audit,
        NotificacionService notifications,
        UsuarioRepository usuarioRepository,
        AvisoDisponibilidadRepository availabilityWatches,
        ConfiguracionRepository configuracionRepository,
        PrestamoReadRepository queries,
        PrestamoEstadoRepository states,
        PrestamoDisponibilidadRepository availability
    )
        : base(repository, validator, mapper, audit)
    {
        _notifications = notifications;
        _usuarioRepository = usuarioRepository;
        _availabilityWatches = availabilityWatches;
        _configuracionRepository = configuracionRepository;
        _queries = queries;
        _states = states;
        _availability = availability;
    }

    public override Task<Result<PrestamoDto>> Create(PrestamoDto dto) =>
        Create(dto, CancellationToken.None);

    public async Task<Result<PrestamoDto>> Create(
        PrestamoDto dto,
        CancellationToken cancellationToken
    )
    {
        var validation = await Validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
            return validation.ToResult<PrestamoDto>();

        var entity = MapToEntity(dto);

        var eligibility = await EvaluateReservation(entity.Carnet!, cancellationToken);

        if (!eligibility.PuedeReservar)
            return Result<PrestamoDto>.Error(eligibility.Motivo!);

        entity.FechaSolicitud = dto.FechaSolicitud ?? DateTime.UtcNow;
        entity.FechaPrestamo = dto.FechaPrestamo ?? dto.FechaPrestamoEsperada;
        entity.FechaPrestamoEsperada = dto.FechaPrestamoEsperada ?? DateTime.UtcNow;
        entity.FechaDevolucionEsperada = dto.FechaDevolucionEsperada ?? DateTime.UtcNow.AddMinutes(30);
        entity.EstadoPrestamo = EstadoPrestamo.Pendiente;

        var createResult = await Repository.CreateReservation(
            entity,
            dto.GrupoEquipoId ?? [],
            dto.Contrato,
            cancellationToken
        );

        if (!createResult.IsSuccess)
            return Result<PrestamoDto>.Error(
                createResult.Errors.FirstOrDefault() ?? "No se pudo crear la reserva"
            );

        var createdLoan = await _queries.Get(entity.Id, cancellationToken);
        if (!createdLoan.IsSuccess)
            return createdLoan;

        var userDisplayName = await _queries.GetUsuarioDisplayName(
            entity.Carnet!,
            cancellationToken
        );
        var equipmentNames = createdLoan.Value.NombreGrupoEquipo
            ?? string.Join(", ", dto.GrupoEquipoId ?? []);
        var loanDetail = JsonSerializer.Serialize(new
        {
            usuarioNombre = userDisplayName,
            usuarioCarnet = entity.Carnet,
            equiposPrestamo = equipmentNames,
            fechaInicio = entity.FechaPrestamoEsperada,
            fechaDevolucion = entity.FechaDevolucionEsperada,
        });

        await Audit!.Log(
            AuditAccion.Crear,
            typeof(PrestamoEntity).Name,
            entity.Id.ToString(CultureInfo.InvariantCulture),
            loanDetail
        );

        await _notifications.CreateForAdmins(
            TipoNotificacion.AdminNuevoPrestamo,
            "Nueva reserva",
            $"{userDisplayName} realizó una reserva.",
            userDisplayName
        );

        return createdLoan;
    }

    public Task<Result<PrestamoDto>> CreateForUser(
        PrestamoDto dto,
        string carnet,
        CancellationToken cancellationToken = default
    )
    {
        dto.CarnetUsuario = carnet;
        dto.AutorizadoPor = null;
        dto.EntregadoPor = null;
        dto.MotivoRechazo = null;
        return Create(dto, cancellationToken);
    }

    public async Task<Result<PrestamoDto>> CancelForUser(int id, string carnet, CancellationToken cancellationToken)
    {
        var loan = await Repository.FindById(id, cancellationToken);
        if (loan == null || loan.Carnet != carnet) return Result<PrestamoDto>.NotFound();
        if (loan.EstadoPrestamo != EstadoPrestamo.Pendiente && loan.EstadoPrestamo != EstadoPrestamo.Aprobado)
            return Result<PrestamoDto>.Error("Solo puedes cancelar préstamos pendientes o aprobados");
        return await UpdateStatus(id, "cancelado", actorCarnet: carnet, cancellationToken: cancellationToken);
    }

    public Task<Result<PrestamoDto>> GetAuthorized(
        int id,
        string carnet,
        bool isAdmin,
        CancellationToken cancellationToken = default
    ) => _queries.GetAuthorized(id, carnet, isAdmin, cancellationToken);

    public async Task<Result<PrestamoDto>> UpdateStatus(
        int id,
        string newStatus,
        string? observacion = null,
        PrestamoDto? body = null,
        string? actorCarnet = null,
        CancellationToken cancellationToken = default
    )
    {
        var loan = await Repository.FindById(id, cancellationToken);

        if (loan == null)
            return Result<PrestamoDto>.NotFound();

        var parsedState = PrestamoState.Parse(newStatus);

        if (!parsedState.HasValue)
            return Result<PrestamoDto>.Error($"Estado '{newStatus}' no reconocido");

        if (parsedState == EstadoPrestamo.Rechazado && string.IsNullOrWhiteSpace(observacion))
            return Result<PrestamoDto>.Error("Indica el motivo del rechazo");
        if (observacion?.Length > 1024)
            return Result<PrestamoDto>.Error("La observación no puede superar 1024 caracteres");

        if (!PrestamoState.CanTransition(loan.EstadoPrestamo, parsedState.Value))
            return Result<PrestamoDto>.Error(
                $"Transición '{PrestamoState.ToText(loan.EstadoPrestamo)}' → '{newStatus}' no permitida"
            );

        if (
            parsedState.Value == EstadoPrestamo.Aprobado
            && loan.FechaPrestamoEsperada <= DateTime.UtcNow
        )
            return Result<PrestamoDto>.Error(
                "No se puede aprobar una reserva cuyo horario de inicio ya venció"
            );

        if (parsedState.Value == EstadoPrestamo.Aprobado)
        {
            var assigned = await _availability.AssignEquiposOnApproval(id, cancellationToken);

            if (!assigned)
                return Result<PrestamoDto>.Error(
                    "No se puede aprobar: no hay equipos disponibles para uno o más grupos en las fechas solicitadas"
                );

        }

        loan.EstadoPrestamo = parsedState.Value;
        var actor = string.IsNullOrWhiteSpace(actorCarnet) ? "Sistema"
            : await _queries.GetUsuarioDisplayName(actorCarnet, cancellationToken);
        if (parsedState == EstadoPrestamo.Aprobado) loan.AutorizadoPor = actor;
        if (parsedState == EstadoPrestamo.Activo) loan.EntregadoPor = actor;
        if (parsedState == EstadoPrestamo.Rechazado) loan.MotivoRechazo = observacion!.Trim();

        if (observacion != null)
            loan.Observacion = observacion;

        if (parsedState.Value == EstadoPrestamo.Finalizado)
            loan.FechaDevolucion = DateTime.UtcNow;

        await Repository.UpdateTracked(loan, cancellationToken);

        string? auditDetail = null;
        string? equipmentObservationMessage = null;
        var hasDamagedEquipment = false;

        if (parsedState.Value == EstadoPrestamo.Finalizado)
        {
            var returnResult = await HandleFinalizadoEquiposRetorno(
                id,
                observacion,
                body,
                cancellationToken
            );
            auditDetail = returnResult.AuditDetail;
            equipmentObservationMessage = returnResult.UserMessage;
            hasDamagedEquipment = returnResult.HasDamagedEquipment;
        }
        else if (!string.IsNullOrWhiteSpace(observacion))
            auditDetail = JsonSerializer.Serialize(new { observacion });

        var auditAction = parsedState.Value switch
        {
            EstadoPrestamo.Aprobado => AuditAccion.Aprobar,
            EstadoPrestamo.Rechazado => AuditAccion.Rechazar,
            EstadoPrestamo.Activo => AuditAccion.Recoger,
            EstadoPrestamo.Finalizado => AuditAccion.Devolver,
            EstadoPrestamo.Cancelado => AuditAccion.Cancelar,
            EstadoPrestamo.Atrasado => AuditAccion.AtrasadoAutomatico,
            _ => AuditAccion.Editar,
        };

        await Audit!.Log(
            auditAction,
            typeof(PrestamoEntity).Name,
            id.ToString(CultureInfo.InvariantCulture),
            auditDetail
        );

        var emitter = actor ?? "Sistema";

        await NotifyStatusChange(
            id,
            loan.Carnet!,
            parsedState.Value,
            observacion,
            equipmentObservationMessage,
            hasDamagedEquipment,
            emitter
        );

        if (parsedState.Value == EstadoPrestamo.Finalizado)
            await UnblockIfNoOverdueLoans(loan.Carnet!, cancellationToken);

        if (ReleasesAvailability(parsedState.Value))
            await NotifyAvailabilityWatches();

        return await _queries.Get(id, cancellationToken);
    }

    public async Task<Result<PrestamoDto>> UpdateObservation(int id, string? observation, CancellationToken token)
    {
        if (observation?.Length > 1024)
            return Result<PrestamoDto>.Error("La observación no puede superar 1024 caracteres");
        var loan = await Repository.FindById(id, token);
        if (loan == null) return Result<PrestamoDto>.NotFound();
        var previous = loan.Observacion;
        loan.Observacion = observation?.Trim();
        await Repository.UpdateTracked(loan, token);
        await Audit!.Log(AuditAccion.Editar, typeof(PrestamoEntity).Name, id.ToString(CultureInfo.InvariantCulture),
            JsonSerializer.Serialize(new { anterior = previous, observacion = loan.Observacion }));
        return await _queries.Get(id, token);
    }

    public async Task<Result<PrestamoDto>> SetSaved(
        int id,
        string carnet,
        bool saved,
        CancellationToken cancellationToken
    )
    {
        var loan = await Repository.FindById(id, cancellationToken);

        if (loan == null || !string.Equals(loan.Carnet, carnet, StringComparison.Ordinal))
            return Result<PrestamoDto>.NotFound();

        loan.Guardado = saved;
        await Repository.UpdateTracked(loan, cancellationToken);
        await Audit!.Log(
            AuditAccion.Editar,
            typeof(PrestamoEntity).Name,
            id.ToString(CultureInfo.InvariantCulture),
            saved ? "Préstamo guardado para volver a solicitar" : "Préstamo quitado de guardados"
        );

        return await _queries.Get(id, cancellationToken);
    }

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

        await _usuarioRepository.SetBlockedStatus([carnet], false, null);
        await Audit!.LogAsSystem(
            AuditAccion.Desbloquear,
            "Usuario",
            carnet,
            "Cuenta desbloqueada automáticamente al regularizar los préstamos atrasados"
        );
        await _notifications.Create(
            carnet,
            TipoNotificacion.UsuarioDesbloqueado,
            "Cuenta desbloqueada para reservas",
            "Tu cuenta fue desbloqueada porque ya no tienes préstamos atrasados.",
            JsonSerializer.Serialize(new
            {
                emisor = "Sistema",
                motivo = "Todos los préstamos atrasados fueron regularizados.",
                fecha = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
            })
        );
    }

    private async Task NotifyAvailabilityWatches()
    {
        var pending = await _availabilityWatches.GetPending();
        var config = await _configuracionRepository.GetConfiguracion();

        if (pending.Count == 0)
            return;

        var notified = new List<int>();
        var notifications = new List<NotificacionDto>();

        foreach (var watch in pending)
        {
            var date = watch.Fecha;

            if (!HorarioReserva.EsValido(date, date.AddMinutes(config.TiempoMinimoReservaMinutos), config))
                continue;

            if (!await _availability.HasAvailableEquipo(
                watch.IdGrupoEquipo,
                date,
                date.AddMinutes(config.TiempoMinimoReservaMinutos)
            ))
                continue;

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

        if (notified.Count == 0)
            return;

        await _notifications.CreateMany(notifications);
        await _availabilityWatches.MarkAsNotified(notified);
    }

    private async Task NotifyStatusChange(
        int prestamoId,
        string carnet,
        EstadoPrestamo estado,
        string? observacion,
        string? userMessage,
        bool hasDamagedEquipment,
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
            case EstadoPrestamo.Finalizado when hasDamagedEquipment:
                await _notifications.Create(
                    carnet,
                    TipoNotificacion.EquipoObservacion,
                    "Equipo marcado como inoperativo",
                    userMessage,
                    NotificacionService.BuildEmitterDetail(
                        emitter,
                        observacion ?? "Equipo devuelto con observaciones.",
                        prestamoId
                    )
                );
                break;
        }
    }

    private async Task<(string? AuditDetail, string? UserMessage, bool HasDamagedEquipment)> HandleFinalizadoEquiposRetorno(
        int id,
        string? observacion,
        PrestamoDto? body,
        CancellationToken cancellationToken
    )
    {
        if (body?.EquiposRetorno == null || body.EquiposRetorno.Count == 0)
            return (null, null, false);

        var statesByCodigoImt = new Dictionary<int, EstadoEquipo>();

        foreach (var item in body.EquiposRetorno)
        {
            if (int.TryParse(item.CodigoImt, out var codigoImt))
                statesByCodigoImt[codigoImt] = ParseEstadoEquipo(item.EstadoEquipo);
        }

        var appliedReturns = await _states.ApplyEstadoEquipoRetorno(
            id,
            statesByCodigoImt,
            cancellationToken
        );
        var affectedEquipment = appliedReturns
            .Where(equipment => equipment.EstadoEquipo != "operativo")
            .ToList();

        var auditDetail = JsonSerializer.Serialize(
            new
            {
                observacion,
                equipos = appliedReturns.Select(appliedReturn => new
                {
                    codigo = appliedReturn.CodigoImt,
                    nombre = appliedReturn.NombreGrupoEquipo,
                    estado = appliedReturn.EstadoEquipo,
                }),
            }
        );

        if (affectedEquipment.Count == 0)
            return (auditDetail, null, false);

        var inoperableEquipment = affectedEquipment
            .Where(equipment => equipment.EstadoEquipo == "inoperativo")
            .ToList();
        var selectedEquipment = inoperableEquipment.Count > 0
            ? inoperableEquipment
            : affectedEquipment;

        var equipmentNames = string.Join(
            ", ",
            selectedEquipment.Select(equipment =>
                $"{equipment.NombreGrupoEquipo ?? "Equipo"} IMT {equipment.CodigoImt}"
            )
        );
        var stateText = inoperableEquipment.Count > 0
            ? "inoperativo"
            : "con observación";
        var message = selectedEquipment.Count == 1
            ? $"Se ha marcado {equipmentNames} como {stateText} en tu préstamo."
            : $"Se han marcado estos equipos como {stateText} en tu préstamo: {equipmentNames}.";

        return (auditDetail, message, true);
    }

    private static EstadoEquipo ParseEstadoEquipo(string? estado) =>
        estado switch
        {
            "parcialmente_operativo" => EstadoEquipo.ParcialmenteOperativo,
            "inoperativo" => EstadoEquipo.Inoperativo,
            "en_mantenimiento" => EstadoEquipo.EnMantenimiento,
            _ => EstadoEquipo.Operativo,
        };

    public async Task<Result<EstadoReservaDto>> GetReservationStatus(
        string carnet,
        CancellationToken cancellationToken = default
    ) => Result<EstadoReservaDto>.Success(
        await EvaluateReservation(carnet, cancellationToken)
    );

    private async Task<EstadoReservaDto> EvaluateReservation(
        string carnet,
        CancellationToken cancellationToken
    )
    {
        if (await _queries.HasAtrasadoPrestamo(carnet, cancellationToken))
            return new EstadoReservaDto
            {
                PuedeReservar = false,
                Motivo =
                    "Tiene un préstamo con devolución atrasada. Devuelva los equipos antes de realizar una nueva reserva.",
            };

        if (await _queries.IsUserBlocked(carnet, cancellationToken))
        {
            var blockReason = await _queries.GetBlockReason(carnet, cancellationToken);

            return new EstadoReservaDto
            {
                PuedeReservar = false,
                Motivo = string.IsNullOrWhiteSpace(blockReason)
                    ? "Cuenta bloqueada para reservas."
                    : $"Cuenta bloqueada para reservas: {blockReason}",
            };
        }

        return new EstadoReservaDto { PuedeReservar = true };
    }

    public async Task<Result<List<PrestamoDto>>> GetHistory(
        string carnetUsuario,
        string estadoPrestamo,
        int page = 1,
        int pageSize = PrestamoReadRepository.MaxPageSize,
        CancellationToken cancellationToken = default,
        bool? guardado = null
    )
    {
        if (string.IsNullOrEmpty(carnetUsuario))
            return Result<List<PrestamoDto>>.Error("Carnet requerido");

        return await GetFiltered(
            carnetUsuario,
            estadoPrestamo,
            page,
            pageSize,
            cancellationToken,
            guardado
        );
    }

    public async Task<Result<List<PrestamoDto>>> GetFiltered(
        string? carnetUsuario,
        string estadoPrestamo,
        int page = 1,
        int pageSize = PrestamoReadRepository.MaxPageSize,
        CancellationToken cancellationToken = default,
        bool? guardado = null
    )
    {

        EstadoPrestamo? estado = null;

        if (!string.IsNullOrEmpty(estadoPrestamo) && estadoPrestamo != "todos")
        {
            estado = PrestamoState.Parse(estadoPrestamo);

            if (!estado.HasValue)
                return Result<List<PrestamoDto>>.Error("Estado préstamo no válido");
        }

        return await _queries.GetHistoryWithDetails(
            carnetUsuario,
            estado,
            page,
            pageSize,
            cancellationToken,
            guardado
        );
    }
}
