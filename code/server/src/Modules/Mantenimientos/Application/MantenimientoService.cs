using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Server.Infrastructure.Jobs;
using MantenimientoEntity = IMT_Reservas.Server.Core.Entities.Mantenimiento;

namespace IMT_Reservas.Server.Application.Features.Mantenimiento;

public class MantenimientoService
    : Service<MantenimientoEntity, MantenimientoRepository, MantenimientoDto>
{
    private readonly EmpresaMantenimientoRepository _empresaRepository;
    private readonly EstadoMantenimientoJob _estadoJob;

    public MantenimientoService(
        MantenimientoRepository repository,
        EmpresaMantenimientoRepository empresaRepository,
        MantenimientoMapper mapper,
        IValidator<MantenimientoDto> validator,
        AuditLogService audit,
        EstadoMantenimientoJob estadoJob
    )
        : base(repository, validator, mapper, audit)
    {
        _empresaRepository = empresaRepository;
        _estadoJob = estadoJob;
    }

    public override async Task<Result<MantenimientoDto>> Create(MantenimientoDto dto)
    {
        await ResolveEmpresa(dto);
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<MantenimientoDto>();

        if (await HasScheduleConflict(dto))
            return Result<MantenimientoDto>.Error(
                "Uno o más equipos ya están reservados o en mantenimiento durante ese horario"
            );

        var entity = MapToEntity(dto);
        var result = await CreateEntity(entity);

        if (!result.IsSuccess)
            return result;

        await Repository.AddDetalles(
            entity.Id,
            dto.CodigoImt ?? [],
            dto.TiposMantenimiento,
            dto.DescripcionesEquipo
        );
        await _estadoJob.Execute(CancellationToken.None);
        await Audit!.Log(
            AuditAccion.Crear,
            typeof(MantenimientoEntity).Name,
            result.Value?.Id?.ToString(CultureInfo.InvariantCulture),
            AuditChangeDetail.BuildCreated(dto)
        );

        return result;
    }

    public override async Task<Result<MantenimientoDto>> Update(int id, MantenimientoDto dto)
    {
        var previous = await Repository.Get(id);
        await ResolveEmpresa(dto);
        var validation = await Validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return validation.ToResult<MantenimientoDto>();

        if (await HasScheduleConflict(dto, id))
            return Result<MantenimientoDto>.Error(
                "Uno o más equipos ya están reservados o en mantenimiento durante ese horario"
            );

        dto.Id = id;
        var updateResult = await UpdateEntity(MapToEntity(dto));
        if (!updateResult.IsSuccess)
            return updateResult;

        if (dto.CodigoImt is { Length: > 0 })
        {
            await Repository.ReplaceDetalles(
                id,
                dto.CodigoImt,
                dto.TiposMantenimiento,
                dto.DescripcionesEquipo
            );
        }

        await _estadoJob.Execute(CancellationToken.None);

        var updated = await Repository.Get(id);

        await Audit!.Log(
            AuditAccion.Editar,
            typeof(MantenimientoEntity).Name,
            id.ToString(CultureInfo.InvariantCulture),
            previous.IsSuccess && updated.IsSuccess
                ? AuditChangeDetail.Build(previous.Value, updated.Value)
                : null
        );

        return updated;
    }

    public override async Task<Result<object>> Delete(int id)
    {
        var result = await base.Delete(id);

        if (result.IsSuccess)
            await _estadoJob.Execute(CancellationToken.None);

        return result;
    }

    private async Task ResolveEmpresa(MantenimientoDto dto)
    {
        if ((dto.IdEmpresa ?? 0) > 0)
            return;
        if (string.IsNullOrWhiteSpace(dto.NombreEmpresaMantenimiento))
            return;

        dto.IdEmpresa = await _empresaRepository.FindIdByNombre(dto.NombreEmpresaMantenimiento);
    }

    private async Task<bool> HasScheduleConflict(MantenimientoDto dto, int? excludedId = null) =>
        dto.FechaMantenimiento.HasValue
        && dto.FechaFinalMantenimiento.HasValue
        && await Repository.HasScheduleConflict(
            dto.CodigoImt ?? [],
            dto.FechaMantenimiento.Value,
            dto.FechaFinalMantenimiento.Value,
            excludedId
        );

}
