using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using PrestamoEntity = UCB_Hold.Server.Modules.Prestamos.Domain.Prestamo;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Configuracion.Application;
using UCB_Hold.Server.Modules.Notificaciones.Application;
using UCB_Hold.Server.Modules.Prestamos.Application.State;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public class PrestamoService : Service<PrestamoEntity, PrestamoRepository, PrestamoDto>
{
    private readonly AuditLogService _audit;
    private readonly NotificacionService _notifications;
    private readonly PrestamoReadRepository _queries;
    private readonly ConfiguracionService _configuracion;
    private readonly ContractHtmlProcessor _contractHtml;

    public PrestamoService(
        PrestamoRepository repository,
        PrestamoMapper mapper,
        IValidator<PrestamoDto> validator,
        AuditLogService audit,
        NotificacionService notifications,
        PrestamoReadRepository queries,
        ConfiguracionService configuracion,
        ContractHtmlProcessor contractHtml
    )
        : base(repository, validator, mapper, audit)
    {
        _audit = audit;
        _notifications = notifications;
        _queries = queries;
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

        var contract = await RenderContract(dto.Contrato, cancellationToken);

        if (contract.Error != null)
            return contract.Error;

        var createResult = await Repository.CreateReservation(
            entity,
            dto.GrupoEquipoId ?? [],
            contract.Html,
            cancellationToken
        );

        if (!createResult.IsSuccess)
            return Result<PrestamoDto>.Error(
                createResult.Errors.FirstOrDefault() ?? "No se pudo crear la reserva"
            );

        var createdLoan = await _queries.Get(entity.Id, cancellationToken);
        if (createdLoan.IsSuccess)
            await RecordCreation(entity, createdLoan.Value, cancellationToken);

        return createdLoan;
    }

    private async Task<(string? Html, Result<PrestamoDto>? Error)> RenderContract(
        string? contract,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(contract))
            return (contract, null);

        try
        {
            var config = await _configuracion.GetConfiguracion(cancellationToken);
            return (_contractHtml.RenderInstitutionalSigner(contract, config), null);
        }
        catch (ArgumentException exception)
        {
            return (
                null,
                Result<PrestamoDto>.Invalid(
                    new Ardalis.Result.ValidationError
                    {
                        Identifier = nameof(PrestamoDto.Contrato),
                        ErrorMessage = exception.Message,
                    }
                )
            );
        }
    }

    private async Task RecordCreation(
        PrestamoEntity entity,
        PrestamoDto createdLoan,
        CancellationToken cancellationToken
    )
    {
        var userDisplayName = await _queries.GetUsuarioDisplayName(
            entity.Carnet!,
            cancellationToken
        );
        var loanDetail = AuditChangeDetail.BuildCreated(new
        {
            Usuario = userDisplayName,
            CarnetUsuario = entity.Carnet,
            Equipos = createdLoan.NombreGrupoEquipo,
            FechaInicio = entity.FechaPrestamoEsperada,
            FechaDevolucion = entity.FechaDevolucionEsperada,
            entity.EstadoPrestamo,
            entity.Observacion,
        });

        await _audit.Log(
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

    public async Task<Result<PrestamoDto>> UpdateObservation(int id, string? observation, CancellationToken token)
    {
        if (observation?.Length > 1024)
            return Result<PrestamoDto>.Error("La observación no puede superar 1024 caracteres");
        var loan = await Repository.FindById(id, token);
        if (loan == null) return Result<PrestamoDto>.NotFound();
        var previous = loan.Observacion;
        loan.Observacion = observation?.Trim();
        await Repository.UpdateTracked(loan, token);
        await _audit.Log(AuditAccion.Editar, typeof(PrestamoEntity).Name, id.ToString(CultureInfo.InvariantCulture),
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
}
