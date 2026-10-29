using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Application.Security;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using ContratoEntity = IMT_Reservas.Server.Core.Entities.Contrato;

namespace IMT_Reservas.Server.Application.Features.Contrato;

public class ContratoService : Service<ContratoEntity, ContratoRepository, ContratoDto>
{
    private readonly PrestamoReadRepository _prestamos;
    private readonly ContractHtmlProcessor _contractHtml;
    private readonly AuditLogService _audit;
    private readonly ConfiguracionService _configuracion;
    private readonly SensitiveDataProtector _sensitiveData;

    public ContratoService(
        ContratoRepository repository,
        ContratoMapper mapper,
        IValidator<ContratoDto> validator,
        PrestamoReadRepository prestamos,
        ContractHtmlProcessor contractHtml,
        AuditLogService audit,
        ConfiguracionService configuracion,
        SensitiveDataProtector sensitiveData
    )
        : base(repository, validator, mapper, audit)
    {
        _prestamos = prestamos;
        _contractHtml = contractHtml;
        _audit = audit;
        _configuracion = configuracion;
        _sensitiveData = sensitiveData;
    }

    public async Task<Result<ContratoDto>> CreateForPrestamo(
        int prestamoId,
        string htmlContent,
        CancellationToken cancellationToken = default
    )
    {
        string sanitizedHtml;

        try
        {
            var config = await _configuracion.GetConfiguracion(cancellationToken);
            sanitizedHtml = _contractHtml.RenderInstitutionalSigner(
                htmlContent,
                config.NombreJefeCarrera,
                config.CarnetJefeCarrera ?? "No registrado",
                config.FirmaJefeCarreraBase64
            );
        }
        catch (ArgumentException exception)
        {
            return Result<ContratoDto>.Error(exception.Message);
        }

        var dto = new ContratoDto { ContratoHtml = sanitizedHtml, PrestamoId = prestamoId };
        var validation = await Validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
            return validation.ToResult<ContratoDto>();

        dto.ContratoHtml = _sensitiveData.Protect(sanitizedHtml);

        var loan = await Repository.FindPrestamoById(prestamoId, cancellationToken);

        if (loan == null)
            return Result<ContratoDto>.Error("Préstamo no existe");

        if (loan.IdContrato.HasValue)
            return Result<ContratoDto>.Error("Contrato ya existe para este préstamo");

        var contract = MapToEntity(dto);
        var result = await Repository.Create(contract);

        if (!result.IsSuccess)
            return Result<ContratoDto>.Error(
                result.Errors.FirstOrDefault() ?? "Error al crear contrato"
            );

        loan.IdContrato = result.Value.Id;
        await Repository.SavePrestamo(loan, cancellationToken);
        await _audit.Log(
            AuditAccion.RegistrarContrato,
            "Prestamo",
            prestamoId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Contrato registrado para el préstamo"
        );

        result.Value.ContratoHtml = sanitizedHtml;
        return result;
    }

    public override async Task<Result<object>> Delete(int prestamoId)
    {
        var result = await Repository.Delete(prestamoId);

        if (result.IsSuccess)
            await _audit.Log(
                AuditAccion.EliminarContrato,
                "Prestamo",
                prestamoId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "Contrato eliminado del préstamo"
            );

        return result;
    }

    public async Task<Result<ContratoDto>> GetByPrestamoId(
        int prestamoId,
        string carnet,
        bool isAdmin,
        CancellationToken cancellationToken = default
    )
    {
        if (!await Repository.CanAccess(prestamoId, carnet, isAdmin, cancellationToken))
            return Result<ContratoDto>.NotFound();

        var result = await Repository.GetEntityByPrestamoId(prestamoId, cancellationToken);

        if (!result.IsSuccess)
            return Result<ContratoDto>.Error(
                result.Errors.FirstOrDefault() ?? "Contrato no encontrado"
            );

        var dto = MapToDto(result.Value);
        dto.ContratoHtml = _contractHtml.RenderEquipment(
            _sensitiveData.Unprotect(dto.ContratoHtml ?? string.Empty),
            await _prestamos.GetContractEquipment(prestamoId, cancellationToken)
        );

        return Result<ContratoDto>.Success(dto);
    }
}
