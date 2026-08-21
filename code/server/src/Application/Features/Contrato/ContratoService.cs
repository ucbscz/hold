using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using ContratoEntity = IMT_Reservas.Server.Core.Entities.Contrato;

namespace IMT_Reservas.Server.Application.Features.Contrato;

public class ContratoService : Service<ContratoEntity, ContratoRepository, ContratoDto>
{
    private readonly PrestamoRepository _prestamos;

    public ContratoService(
        ContratoRepository repository,
        ContratoMapper mapper,
        IValidator<ContratoDto> validator,
        PrestamoRepository prestamos
    )
        : base(repository, validator, mapper) => _prestamos = prestamos;

    public async Task<Result<ContratoDto>> CreateForPrestamo(int prestamoId, string htmlContent)
    {
        var dto = new ContratoDto { ContratoHtml = htmlContent, PrestamoId = prestamoId };
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<ContratoDto>();

        var loan = await Repository.FindPrestamoById(prestamoId);

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
        await Repository.SavePrestamo(loan);

        return result;
    }

    public async Task<Result<ContratoDto>> GetByPrestamoId(int prestamoId)
    {
        await _prestamos.UpdateContratoWithEquipos(prestamoId);
        var result = await Repository.GetEntityByPrestamoId(prestamoId);

        if (!result.IsSuccess)
            return Result<ContratoDto>.Error(
                result.Errors.FirstOrDefault() ?? "Contrato no encontrado"
            );

        return Result<ContratoDto>.Success(MapToDto(result.Value));
    }
}
