using Ardalis.Result;
using FluentValidation;
using ContratoEntity = UCB_Hold.Server.Modules.Prestamos.Domain.Contrato;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Configuracion.Application;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public class ContratoService : Service<ContratoEntity, ContratoRepository, ContratoDto>
{
    private const string HtmlContentType = "text/html";
    private const string HtmlExtension = ".html";

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

    public async Task<Result<ContratoDto>> CreateFromFile(
        int prestamoId,
        string? fileName,
        string? contentType,
        long length,
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        if (length == 0)
            return InvalidFile("Archivo HTML requerido");

        if (length > ContractHtmlProcessor.MaxHtmlLength)
            return InvalidFile("El contrato supera el tamaño máximo permitido");

        if (
            !string.Equals(contentType, HtmlContentType, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(fileName), HtmlExtension, StringComparison.OrdinalIgnoreCase)
        )
            return InvalidFile("El contrato debe ser un archivo HTML");

        using var reader = new StreamReader(content);
        var htmlContent = await reader.ReadToEndAsync(cancellationToken);
        return await CreateForPrestamo(prestamoId, htmlContent, cancellationToken);
    }

    private static Result<ContratoDto> InvalidFile(string message) =>
        Result<ContratoDto>.Invalid(new Ardalis.Result.ValidationError { Identifier = "archivo", ErrorMessage = message });

    public async Task<Result<ContratoDto>> CreateForPrestamo(
        int prestamoId,
        string htmlContent,
        CancellationToken cancellationToken = default
    )
    {
        string sanitizedHtml;

        try
        {
            sanitizedHtml = _contractHtml.RenderInstitutionalSigner(
                htmlContent,
                await _configuracion.GetConfiguracion(cancellationToken)
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

        await LinkToLoan(loan, result.Value.Id, cancellationToken);
        result.Value.ContratoHtml = sanitizedHtml;
        return result;
    }

    private async Task LinkToLoan(Domain.Prestamo loan, int? contractId, CancellationToken cancellationToken)
    {
        loan.IdContrato = contractId;
        await Repository.SavePrestamo(loan, cancellationToken);
        await _audit.Log(
            AuditAccion.RegistrarContrato,
            "Prestamo",
            loan.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Contrato registrado para el préstamo"
        );
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
