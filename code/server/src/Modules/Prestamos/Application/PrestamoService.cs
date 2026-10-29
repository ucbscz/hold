using System.Text.Json;
using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using PrestamoEntity = IMT_Reservas.Server.Modules.Prestamos.Domain.Prestamo;
using IMT_Reservas.Server.Modules.Auditoria.Application;
using IMT_Reservas.Server.Modules.Configuracion.Application;
using IMT_Reservas.Server.Modules.Configuracion.Infrastructure;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Modules.Notificaciones.Application;
using IMT_Reservas.Server.Modules.Prestamos.Application.State;
using IMT_Reservas.Server.Modules.Prestamos.Domain;
using IMT_Reservas.Server.Modules.Prestamos.Infrastructure;
using IMT_Reservas.Server.Modules.Usuarios.Application;
using IMT_Reservas.Server.Modules.Usuarios.Infrastructure;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Prestamos.Application;

public class PrestamoService : Service<PrestamoEntity, PrestamoRepository, PrestamoDto>
{
    private readonly NotificacionService _notifications;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly AvisoDisponibilidadService _availabilityWatches;
    private readonly ConfiguracionRepository _configuracionRepository;
    private readonly PrestamoReadRepository _queries;
    private readonly PrestamoEstadoRepository _states;
    private readonly PrestamoDisponibilidadRepository _availability;
    private readonly ConfiguracionService _configuracion;
    private readonly ContractHtmlProcessor _contractHtml;
    private readonly EquipmentLabels _labels;

    public PrestamoService(
        PrestamoRepository repository,
        PrestamoMapper mapper,
        IValidator<PrestamoDto> validator,
        AuditLogService audit,
        NotificacionService notifications,
        UsuarioRepository usuarioRepository,
        AvisoDisponibilidadService availabilityWatches,
        ConfiguracionRepository configuracionRepository,
        PrestamoReadRepository queries,
        PrestamoEstadoRepository states,
        PrestamoDisponibilidadRepository availability,
        ConfiguracionService configuracion,
        ContractHtmlProcessor contractHtml,
        EquipmentLabels labels
    )
        : base(repository, validator, mapper, audit)
    {
        _labels = labels;
        _notifications = notifications;
        _usuarioRepository = usuarioRepository;
        _availabilityWatches = availabilityWatches;
        _configuracionRepository = configuracionRepository;
        _queries = queries;
        _states = states;
        _availability = availability;
        _configuracion = configuracion;
        _contractHtml = contractHtml;
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
        entity.FechaPrestamoEsperada = dto.FechaPrestamoEsperada!.Value;
        entity.FechaDevolucionEsperada = dto.FechaDevolucionEsperada!.Value;
        entity.EstadoPrestamo = EstadoPrestamo.Pendiente;

        var contract = dto.Contrato;
        if (!string.IsNullOrWhiteSpace(contract))
        {
            try
            {
                var config = await _configuracion.GetConfiguracion(cancellationToken);
                contract = _contractHtml.RenderInstitutionalSigner(
                    contract,
                    config.NombreJefeCarrera,
                    config.CarnetJefeCarrera ?? "No registrado",
                    config.FirmaJefeCarreraBase64
                );
            }
            catch (ArgumentException exception)
            {
                return Result<PrestamoDto>.Invalid(
                    new Ardalis.Result.ValidationError
                    {
                        Identifier = nameof(dto.Contrato),
                        ErrorMessage = exception.Message,
                    }
                );
            }
        }

        var createResult = await Repository.CreateReservation(
            entity,
            dto.GrupoEquipoId ?? [],
            contract,
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
        var loanDetail = AuditChangeDetail.BuildCreated(new
        {
            Usuario = userDisplayName,
            CarnetUsuario = entity.Carnet,
            Equipos = createdLoan.Value.NombreGrupoEquipo,
            FechaInicio = entity.FechaPrestamoEsperada,
            FechaDevolucion = entity.FechaDevolucionEsperada,
            entity.EstadoPrestamo,
            entity.Observacion,
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
        dto.Id = null;
        dto.CarnetUsuario = carnet;
        dto.FechaSolicitud = null;
        dto.FechaPrestamo = null;
        dto.FechaDevolucion = null;
        dto.Observacion = null;
        dto.IdContrato = null;
        dto.Guardado = false;
        dto.AutorizadoPor = null;
        dto.EntregadoPor = null;
        dto.MotivoRechazo = null;
        return Create(dto, cancellationToken);
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
        var loan = await Repository.FindById(id, cancellationToken);
        if (loan == null || loan.Carnet != carnet) return Result<PrestamoDto>.NotFound();
        if (loan.EstadoPrestamo != EstadoPrestamo.Pendiente && loan.EstadoPrestamo != EstadoPrestamo.Aprobado)
            return Result<PrestamoDto>.Error("Solo puedes cancelar préstamos pendientes o aprobados");
        return await UpdateStatus(id, PrestamoState.ToText(EstadoPrestamo.Cancelado), actorCarnet: carnet, cancellationToken: cancellationToken);
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

        var actor = string.IsNullOrWhiteSpace(actorCarnet) ? SystemActor.Nombre
            : await _queries.GetUsuarioDisplayName(actorCarnet, cancellationToken);

        if (parsedState.Value == EstadoPrestamo.Aprobado)
        {
            var assigned = await _availability.AssignEquiposOnApproval(
                id,
                actor,
                cancellationToken
            );

            if (!assigned)
                return Result<PrestamoDto>.Error(
                    "No se puede aprobar: no hay equipos disponibles para uno o más grupos en las fechas solicitadas"
                );

        }

        var previousAuditState = new
        {
            loan.EstadoPrestamo,
            loan.Observacion,
            loan.MotivoRechazo,
            loan.AutorizadoPor,
            loan.EntregadoPor,
        };
        loan.EstadoPrestamo = parsedState.Value;
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
                previousAuditState.EstadoPrestamo,
                cancellationToken
            );
            auditDetail = returnResult.AuditDetail;
            equipmentObservationMessage = returnResult.UserMessage;
            hasDamagedEquipment = returnResult.HasDamagedEquipment;
        }
        else
            auditDetail = AuditChangeDetail.Build(
                previousAuditState,
                new
                {
                    loan.EstadoPrestamo,
                    loan.Observacion,
                    loan.MotivoRechazo,
                    loan.AutorizadoPor,
                    loan.EntregadoPor,
                }
            );

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

        var emitter = actor ?? SystemActor.Nombre;

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
            await _availabilityWatches.NotifyAvailable(
                await _configuracionRepository.GetConfiguracion(cancellationToken),
                cancellationToken
            );

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
            AuditChangeDetail.Build(
                new { Observacion = previous },
                new { Observacion = loan.Observacion }
            ));
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

        await _usuarioRepository.SetBlockedStatus([carnet], false, null, cancellationToken);
        await Audit!.LogAsSystem(
            AuditAccion.Desbloquear,
            "Usuario",
            carnet,
            AuditChangeDetail.Build(
                new { Bloqueado = true, MotivoBloqueo = (string?)"Préstamos atrasados" },
                new { Bloqueado = false, MotivoBloqueo = (string?)null }
            )
        );
        await _notifications.Create(
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
        EstadoPrestamo previousState,
        CancellationToken cancellationToken
    )
    {
        if (body?.EquiposRetorno == null || body.EquiposRetorno.Count == 0)
            return (
                AuditChangeDetail.Build(
                    new { EstadoPrestamo = previousState },
                    new { EstadoPrestamo = EstadoPrestamo.Finalizado }
                ),
                null,
                false
            );

        var statesByCodigoImt = new Dictionary<int, EstadoEquipo>();

        foreach (var item in body.EquiposRetorno)
        {
            if (int.TryParse(item.CodigoImt, out var codigoImt))
                statesByCodigoImt[codigoImt] = EstadoEquipoNombre.Parse(item.EstadoEquipo);
        }

        var appliedReturns = await _states.ApplyEstadoEquipoRetorno(
            id,
            statesByCodigoImt,
            cancellationToken
        );
        var affectedEquipment = appliedReturns
            .Where(equipment => equipment.EstadoEquipo != EstadoEquipoNombre.Operativo)
            .ToList();

        var auditDetail = JsonSerializer.Serialize(
            new
            {
                observacion,
                cambios = new[]
                {
                    new
                    {
                        campo = "Estado del préstamo",
                        protegido = false,
                        anterior = PrestamoState.ToText(previousState),
                        nuevo = PrestamoState.ToText(EstadoPrestamo.Finalizado),
                    },
                },
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
            .Where(equipment => equipment.EstadoEquipo == EstadoEquipoNombre.Inoperativo)
            .ToList();
        var selectedEquipment = inoperableEquipment.Count > 0
            ? inoperableEquipment
            : affectedEquipment;

        var equipmentNames = string.Join(
            ", ",
            selectedEquipment.Select(equipment =>
                $"{equipment.NombreGrupoEquipo ?? "Equipo"} {_labels.InternalCode(equipment.CodigoImt)}"
            )
        );
        var stateText = inoperableEquipment.Count > 0
            ? EstadoEquipoNombre.Inoperativo
            : "con observación";
        var message = selectedEquipment.Count == 1
            ? $"Se ha marcado {equipmentNames} como {stateText} en tu préstamo."
            : $"Se han marcado estos equipos como {stateText} en tu préstamo: {equipmentNames}.";

        return (auditDetail, message, true);
    }


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
        bool? guardado = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(carnetUsuario))
            return Result<List<PrestamoDto>>.Error("Carnet requerido");

        return await GetFiltered(
            carnetUsuario,
            estadoPrestamo,
            page,
            pageSize,
            guardado,
            cancellationToken
        );
    }

    public async Task<Result<List<PrestamoDto>>> GetFiltered(
        string? carnetUsuario,
        string estadoPrestamo,
        int page = 1,
        int pageSize = PrestamoReadRepository.MaxPageSize,
        bool? guardado = null,
        CancellationToken cancellationToken = default
    )
    {

        EstadoPrestamo? estado = null;

        if (!string.IsNullOrEmpty(estadoPrestamo) && estadoPrestamo != PrestamoState.TodosFiltro)
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
            guardado,
            cancellationToken
        );
    }
}
