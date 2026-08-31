using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using EquipoEntity = IMT_Reservas.Server.Core.Entities.Equipo;

namespace IMT_Reservas.Server.Application.Features.Equipo;

public class EquipoService : Service<EquipoEntity, EquipoRepository, EquipoDto>
{
    public EquipoService(
        EquipoRepository repository,
        EquipoMapper mapper,
        IValidator<EquipoDto> validator,
        AuditLogService audit
    )
        : base(repository, validator, mapper, audit) { }

    public override async Task<Result<EquipoDto>> Create(EquipoDto dto)
    {
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<EquipoDto>();

        var entity = MapToEntity(dto);
        entity.CodigoImt = await Repository.GetMaxCodigoImt() + 1;
        entity.FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Now);

        entity.CodigoUcb = string.IsNullOrWhiteSpace(entity.CodigoUcb) ? null : entity.CodigoUcb.Trim().ToUpperInvariant();
        if (entity.CodigoUcb != null && await Repository.ExistsByCodigoUcb(entity.CodigoUcb))
            return Result<EquipoDto>.Error("Código UCB ya registrado");

        if (await Repository.ExistsByCodigoImt(entity.CodigoImt))
            return Result<EquipoDto>.Error("Código IMT ya registrado");

        var result = await CreateEntity(entity);

        if (result.IsSuccess)
        {
            await Repository.RecalcGrupoStats(entity.IdGrupoEquipo);
            await Audit!.Log(
                AuditAccion.Crear,
                typeof(EquipoEntity).Name,
                result.Value?.Id?.ToString(CultureInfo.InvariantCulture)
            );
        }

        return result;
    }

    public override async Task<Result<EquipoDto>> Update(int id, EquipoDto dto)
    {
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<EquipoDto>();

        var existing = await Repository.FindById(id);

        if (existing == null)
            return Result<EquipoDto>.NotFound();

        var entity = MapToEntity(dto);
        entity.Id = id;
        entity.CodigoImt = existing.CodigoImt;
        entity.FechaIngresoEquipo = existing.FechaIngresoEquipo;
        entity.EstadoEliminado = existing.EstadoEliminado;

        entity.CodigoUcb = string.IsNullOrWhiteSpace(entity.CodigoUcb) ? null : entity.CodigoUcb.Trim().ToUpperInvariant();
        if (entity.CodigoUcb != null && await Repository.ExistsByCodigoUcb(entity.CodigoUcb, id))
            return Result<EquipoDto>.Error("Código UCB ya registrado");

        var result = await UpdateEntity(entity);

        if (!result.IsSuccess)
            return result;

        await Repository.RecalcGrupoStats(entity.IdGrupoEquipo);

        if (existing.IdGrupoEquipo != entity.IdGrupoEquipo)
            await Repository.RecalcGrupoStats(existing.IdGrupoEquipo);

        await Audit!.Log(
            AuditAccion.Editar,
            typeof(EquipoEntity).Name,
            id.ToString(CultureInfo.InvariantCulture)
        );

        return result;
    }

    public override async Task<Result<object>> Delete(int id)
    {
        var existing = await Repository.FindById(id);

        if (existing == null)
            return Result<object>.NotFound();

        var result = await base.Delete(id);

        if (result.IsSuccess)
            await Repository.RecalcGrupoStats(existing.IdGrupoEquipo);

        return result;
    }

    public virtual async Task<Result<List<EquipoDto>>> GetByGrupo(int grupoId) =>
        Result<List<EquipoDto>>.Success(await Repository.GetByGrupo(grupoId));

    public virtual async Task<Result<List<EquipoDto>>> GetByGavetero(int gaveteroId) =>
        Result<List<EquipoDto>>.Success(await Repository.GetByGavetero(gaveteroId));

    public virtual async Task<Result<List<HistorialEquipoDto>>> GetHistorial(int equipoId) =>
        Result<List<HistorialEquipoDto>>.Success(await Repository.GetHistorial(equipoId));

}
