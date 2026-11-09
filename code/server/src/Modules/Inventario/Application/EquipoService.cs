using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using EquipoEntity = UCB_Hold.Server.Modules.Inventario.Domain.Equipo;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Inventario.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

public class EquipoService : Service<EquipoEntity, EquipoRepository, EquipoDto>
{
    private readonly EquipmentLabels _labels;
    private readonly IHistorialPrestamosEquipo _historial;

    public EquipoService(
        EquipoRepository repository,
        EquipoMapper mapper,
        IValidator<EquipoDto> validator,
        AuditLogService audit,
        EquipmentLabels labels,
        IHistorialPrestamosEquipo historial
    )
        : base(repository, validator, mapper, audit)
    {
        _labels = labels;
        _historial = historial;
    }

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
            return Result<EquipoDto>.Error(_labels.DuplicateOrganizationCode);

        if (await Repository.ExistsByCodigoImt(entity.CodigoImt))
            return Result<EquipoDto>.Error(_labels.DuplicateInternalCode);

        var result = await CreateEntity(entity);

        if (result.IsSuccess)
        {
            await Repository.RecalcGrupoStats(entity.IdGrupoEquipo);
            await Audit!.Log(
                AuditAccion.Crear,
                typeof(EquipoEntity).Name,
                result.Value?.Id?.ToString(CultureInfo.InvariantCulture),
                AuditChangeDetail.BuildCreated(result.Value)
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

        var previous = MapToDto(existing);

        var entity = MergeWithExisting(dto, existing);
        if (entity.CodigoUcb != null && await Repository.ExistsByCodigoUcb(entity.CodigoUcb, id))
            return Result<EquipoDto>.Error(_labels.DuplicateOrganizationCode);

        var result = await UpdateEntity(entity);

        if (!result.IsSuccess)
            return result;

        await Repository.RecalcGrupoStats(entity.IdGrupoEquipo);

        if (existing.IdGrupoEquipo != entity.IdGrupoEquipo)
            await Repository.RecalcGrupoStats(existing.IdGrupoEquipo);

        await Audit!.Log(
            AuditAccion.Editar,
            typeof(EquipoEntity).Name,
            id.ToString(CultureInfo.InvariantCulture),
            AuditChangeDetail.Build(previous, result.Value)
        );

        return result;
    }

    private EquipoEntity MergeWithExisting(EquipoDto dto, EquipoEntity existing)
    {
        var entity = MapToEntity(dto);
        entity.Id = existing.Id;
        entity.CodigoImt = existing.CodigoImt;
        entity.FechaIngresoEquipo = existing.FechaIngresoEquipo;
        entity.EstadoEliminado = existing.EstadoEliminado;
        entity.CodigoUcb = string.IsNullOrWhiteSpace(entity.CodigoUcb)
            ? null
            : entity.CodigoUcb.Trim().ToUpperInvariant();
        return entity;
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
        Result<List<HistorialEquipoDto>>.Success(await _historial.GetHistorial(equipoId));

}
