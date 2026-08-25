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

    public PrestamoService(
        PrestamoRepository repository,
        PrestamoMapper mapper,
        IValidator<PrestamoDto> validator,
        AuditLogService audit,
        NotificacionService notifications,
        UsuarioRepository usuarioRepository,
        AvisoDisponibilidadRepository availabilityWatches,
        ConfiguracionRepository configuracionRepository
    )
        : base(repository, validator, mapper, audit)
    {
        _notifications = notifications;
        _usuarioRepository = usuarioRepository;
        _availabilityWatches = availabilityWatches;
        _configuracionRepository = configuracionRepository;
    }

    public override async Task<Result<PrestamoDto>> Create(PrestamoDto dto)
    {
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<PrestamoDto>();

        var entity = MapToEntity(dto);

        var eligibility = await EvaluateReservation(entity.Carnet!);

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
            dto.Contrato
        );

        if (!createResult.IsSuccess)
            return Result<PrestamoDto>.Error(
                createResult.Errors.FirstOrDefault() ?? "No se pudo crear la reserva"
            );

        var createdLoan = await Repository.Get(entity.Id);
        if (!createdLoan.IsSuccess)
            return createdLoan;

        var userDisplayName = await Repository.GetUsuarioDisplayName(entity.Carnet!);
        var equipmentNames = createdLoan.Value.NombreGrupoEquipo
            ?? string.Join(", ", dto.GrupoEquipoId ?? []);
        var loanDetail = JsonSerializer.Serialize(new
        {
            texto = $"Usuario: {userDisplayName} ({entity.Carnet}). Equipos: {equipmentNames}. Inicio: {entity.FechaPrestamoEsperada:yyyy-MM-dd HH:mm} UTC. Devolución: {entity.FechaDevolucionEsperada:yyyy-MM-dd HH:mm} UTC.",
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

    public Task<Result<PrestamoDto>> CreateForUser(PrestamoDto dto, string carnet)
    {
        dto.CarnetUsuario = carnet;
        return Create(dto);
    }

    public Task<Result<PrestamoDto>> GetAuthorized(int id, string carnet, bool isAdmin) =>
        Repository.GetAuthorized(id, carnet, isAdmin);

    public async Task<Result<PrestamoDto>> UpdateStatus(
        int id,
        string newStatus,
        string? observacion = null,
        PrestamoDto? body = null,
        string? actorCarnet = null
    )
    {
        var loan = await Repository.FindById(id);

        if (loan == null)
            return Result<PrestamoDto>.NotFound();

        var parsedState = PrestamoState.Parse(newStatus);

        if (!parsedState.HasValue)
            return Result<PrestamoDto>.Error($"Estado '{newStatus}' no reconocido");

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
            var assigned = await Repository.AssignEquiposOnApproval(id);

            if (!assigned)
                return Result<PrestamoDto>.Error(
                    "No se puede aprobar: no hay equipos disponibles para uno o más grupos en las fechas solicitadas"
                );

        }

        loan.EstadoPrestamo = parsedState.Value;

        if (observacion != null)
            loan.Observacion = observacion;

        if (parsedState.Value == EstadoPrestamo.Finalizado)
            loan.FechaDevolucion = DateTime.UtcNow;

        await Repository.UpdateTracked(loan);

        string? auditDetail = null;
        string? equipmentObservationMessage = null;
        var hasDamagedEquipment = false;

        if (parsedState.Value == EstadoPrestamo.Finalizado)
        {
            var returnResult = await HandleFinalizadoEquiposRetorno(id, observacion, body);
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

        var emitter = string.IsNullOrWhiteSpace(actorCarnet)
            ? "Sistema"
            : await _usuarioRepository.GetDisplayName(actorCarnet) ?? "Sistema";

        await NotifyStatusChange(
            loan.Carnet!,
            parsedState.Value,
            observacion,
            equipmentObservationMessage,
            hasDamagedEquipment,
            emitter
        );

        if (parsedState.Value == EstadoPrestamo.Finalizado)
            await UnblockIfNoOverdueLoans(loan.Carnet!);

        if (ReleasesAvailability(parsedState.Value))
            await NotifyAvailabilityWatches();

        return await Get(id);
    }

    private static bool ReleasesAvailability(EstadoPrestamo estado) =>
        estado is EstadoPrestamo.Finalizado or EstadoPrestamo.Cancelado or EstadoPrestamo.Rechazado;

    private async Task UnblockIfNoOverdueLoans(string carnet)
    {
        if (await Repository.HasAtrasadoPrestamo(carnet))
            return;

        var blockReason = await Repository.GetBlockReason(carnet);

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

            if (!HorarioReserva.EsValido(date, date.AddMinutes(config.TiempoMinimoReservaMinutos), config.HorarioInicioMinutos, config.HorarioFinMinutos))
                continue;

            if (!await Repository.HasAvailableEquipo(
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
                await _notifications.Create(
                    carnet,
                    TipoNotificacion.PrestamoAprobado,
                    "Préstamo aprobado",
                    "Tu solicitud de préstamo fue aprobada. Ya puedes revisar los detalles de recogida.",
                    NotificacionService.BuildEmitterDetail(emitter, "Solicitud aprobada.")
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
                        observacion ?? "Solicitud rechazada."
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
                        observacion ?? "Equipo devuelto con observaciones."
                    )
                );
                break;
        }
    }

    private async Task<(string? AuditDetail, string? UserMessage, bool HasDamagedEquipment)> HandleFinalizadoEquiposRetorno(
        int id,
        string? observacion,
        PrestamoDto? body
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

        var appliedReturns = await Repository.ApplyEstadoEquipoRetorno(id, statesByCodigoImt);
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
            _ => EstadoEquipo.Operativo,
        };

    public async Task<Result<EstadoReservaDto>> GetReservationStatus(string carnet) =>
        Result<EstadoReservaDto>.Success(await EvaluateReservation(carnet));

    private async Task<EstadoReservaDto> EvaluateReservation(string carnet)
    {
        if (await Repository.HasAtrasadoPrestamo(carnet))
            return new EstadoReservaDto
            {
                PuedeReservar = false,
                Motivo =
                    "Tiene un préstamo con devolución atrasada. Devuelva los equipos antes de realizar una nueva reserva.",
            };

        if (await Repository.IsUserBlocked(carnet))
        {
            var blockReason = await Repository.GetBlockReason(carnet);

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
        string estadoPrestamo
    )
    {
        if (string.IsNullOrEmpty(carnetUsuario))
            return Result<List<PrestamoDto>>.Error("Carnet requerido");

        return await GetFiltered(carnetUsuario, estadoPrestamo);
    }

    public async Task<Result<List<PrestamoDto>>> GetFiltered(
        string? carnetUsuario,
        string estadoPrestamo
    )
    {

        EstadoPrestamo? estado = null;

        if (!string.IsNullOrEmpty(estadoPrestamo) && estadoPrestamo != "todos")
        {
            estado = PrestamoState.Parse(estadoPrestamo);

            if (!estado.HasValue)
                return Result<List<PrestamoDto>>.Error("Estado préstamo no válido");
        }

        return await Repository.GetHistoryWithDetails(carnetUsuario, estado);
    }
}
