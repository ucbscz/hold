using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using GaveteroEntity = IMT_Reservas.Server.Core.Entities.Gavetero;

namespace IMT_Reservas.Server.Application.Features.Gavetero;

public class GaveteroService : Service<GaveteroEntity, GaveteroRepository, GaveteroDto>
{
    public GaveteroService(
        GaveteroRepository repository,
        GaveteroMapper mapper,
        IValidator<GaveteroDto> validator,
        AuditLogService audit
    )
        : base(repository, validator, mapper, audit) { }

    public override async Task<Result<GaveteroDto>> Create(GaveteroDto dto)
    {
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<GaveteroDto>();

        var muebleId = await Repository.GetMuebleByNombre(dto.NombreMueble!) ?? 0;
        var entity = MapToEntity(dto);
        entity.IdMueble = muebleId;
        var result = await CreateEntity(entity);

        if (result.IsSuccess)
        {
            await Repository.RecalcMuebleCount(muebleId);
            await Audit!.Log(
                AuditAccion.Crear,
                typeof(GaveteroEntity).Name,
                result.Value?.Id?.ToString(CultureInfo.InvariantCulture),
                AuditChangeDetail.BuildCreated(result.Value)
            );
        }

        return result;
    }

    public override async Task<Result<GaveteroDto>> Update(int id, GaveteroDto dto)
    {
        var previous = await Repository.Get(id);
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<GaveteroDto>();

        var previousMuebleId = await Repository.GetMuebleByGavetero(id);
        var muebleId = !string.IsNullOrWhiteSpace(dto.NombreMueble)
            ? await Repository.GetMuebleByNombre(dto.NombreMueble) ?? 0
            : previousMuebleId ?? 0;

        var entity = MapToEntity(dto);
        entity.Id = id;
        entity.IdMueble = muebleId;
        var result = await UpdateEntity(entity);

        if (!result.IsSuccess)
            return result;

        await Repository.RecalcMuebleCount(muebleId);

        if (previousMuebleId.HasValue && previousMuebleId.Value != muebleId)
            await Repository.RecalcMuebleCount(previousMuebleId.Value);

        await Audit!.Log(
            AuditAccion.Editar,
            typeof(GaveteroEntity).Name,
            id.ToString(CultureInfo.InvariantCulture),
            previous.IsSuccess ? AuditChangeDetail.Build(previous.Value, result.Value) : null
        );
        return result;
    }

    public override async Task<Result<object>> Delete(int id)
    {
        var muebleId = await Repository.GetMuebleByGavetero(id);
        var result = await base.Delete(id);

        if (result.IsSuccess && muebleId.HasValue)
            await Repository.RecalcMuebleCount(muebleId.Value);

        return result;
    }

    public async Task<Result<List<GaveteroDto>>> GetByMueble(int muebleId) =>
        Result<List<GaveteroDto>>.Success(await Repository.GetByMueble(muebleId));
}
