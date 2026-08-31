using System.Globalization;
using IMT_Reservas.Server.Application.Features.Componente;
using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Cache;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using GrupoEquipoEntity = IMT_Reservas.Server.Core.Entities.GrupoEquipo;

namespace IMT_Reservas.Server.Application.Features.GrupoEquipo;

public class GrupoEquipoService : Service<GrupoEquipoEntity, GrupoEquipoRepository, GrupoEquipoDto>
{
    private static readonly TimeSpan GrupoEquipoSearchTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(30);
    private readonly CacheRepository _cacheRepository;
    private readonly ISearchIndex<GrupoEquipoDto> _searchIndex;

    public GrupoEquipoService(
        GrupoEquipoRepository repository,
        GrupoEquipoMapper mapper,
        IValidator<GrupoEquipoDto> validator,
        CacheRepository cacheRepository,
        AuditLogService audit,
        ISearchIndex<GrupoEquipoDto> searchIndex
    )
        : base(repository, validator, mapper, audit)
    {
        _cacheRepository = cacheRepository;
        _searchIndex = searchIndex;
    }

    public override async Task<Result<GrupoEquipoDto>> Create(GrupoEquipoDto dto)
    {
        await ResolveCategoria(dto);
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<GrupoEquipoDto>();

        var createResult = await CreateEntity(MapToEntity(dto));

        if (createResult.IsSuccess)
        {
            await BumpSearchVersion();
            await IndexGrupo(createResult.Value);
            await Audit!.Log(
                AuditAccion.Crear,
                typeof(GrupoEquipoEntity).Name,
                createResult.Value?.Id?.ToString(CultureInfo.InvariantCulture)
            );
        }

        return createResult;
    }

    public override async Task<Result<GrupoEquipoDto>> Update(int id, GrupoEquipoDto dto)
    {
        dto.Id = id;
        await ResolveCategoria(dto);
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<GrupoEquipoDto>();

        var entity = MapToEntity(dto);
        entity.Id = id;

        var updateResult = await UpdateEntity(entity);

        if (updateResult.IsSuccess)
        {
            await BumpSearchVersion();
            await IndexGrupo(updateResult.Value);
            await Audit!.Log(
                AuditAccion.Editar,
                typeof(GrupoEquipoEntity).Name,
                id.ToString(CultureInfo.InvariantCulture)
            );
        }

        return updateResult;
    }

    public override async Task<Result<object>> Delete(int id)
    {
        var deleteResult = await base.Delete(id);

        if (deleteResult.IsSuccess)
        {
            await BumpSearchVersion();
            await _searchIndex.Remove(id);
        }

        return deleteResult;
    }

    public virtual async Task<Result<List<GrupoEquipoDto>>> Search(
        string? nombre = null,
        string? categoria = null
    )
    {
        if (_searchIndex.Enabled && !string.IsNullOrWhiteSpace(nombre))
            return await SearchInIndex(nombre, categoria);

        return await SearchInDatabase(nombre, categoria);
    }

    private async Task<Result<List<GrupoEquipoDto>>> SearchInIndex(string nombre, string? categoria)
    {
        var searchResult = await _searchIndex.Search(
            new SearchQuery(nombre, ["Nombre", "Modelo", "Marca"])
        );

        if (!searchResult.IsSuccess)
            return await SearchInDatabase(nombre, categoria);

        return Result<List<GrupoEquipoDto>>.Success(
            await Repository.GetByIds([.. searchResult.Value], categoria)
        );
    }

    private async Task<Result<List<GrupoEquipoDto>>> SearchInDatabase(
        string? nombre,
        string? categoria
    )
    {
        var version = await GetSearchVersion();
        var cacheKey = CacheKeys.GrupoEquipoSearch(nombre ?? string.Empty, categoria, version);
        var cacheResult = await _cacheRepository.Get<List<GrupoEquipoDto>>(cacheKey);

        if (cacheResult.IsSuccess)
            return Result<List<GrupoEquipoDto>>.Success(cacheResult.Value);

        var results = await Repository.Search(nombre, categoria);
        _ = await _cacheRepository.Set(cacheKey, results, GrupoEquipoSearchTtl);

        return Result<List<GrupoEquipoDto>>.Success(results);
    }

    public async Task<Result<List<ComponenteDto>>> GetComponents(int id, int page, CancellationToken token)
    {
        if (id <= 0 || page < 1 || page > 10000) return Result<List<ComponenteDto>>.Error("Página o grupo no válido");
        return Result<List<ComponenteDto>>.Success(await Repository.GetComponents(id, page, token));
    }

    private async Task IndexGrupo(GrupoEquipoDto? group)
    {
        if (group?.Id is int id)
            await _searchIndex.Index(id, group);
    }

    private async Task<long> GetSearchVersion()
    {
        var result = await _cacheRepository.Get<long>(CacheKeys.GrupoEquipoVersion);
        return result.IsSuccess ? result.Value : 0;
    }

    private async Task BumpSearchVersion()
    {
        var next = await GetSearchVersion() + 1;
        _ = await _cacheRepository.Set(CacheKeys.GrupoEquipoVersion, next, VersionTtl);
    }

    private async Task ResolveCategoria(GrupoEquipoDto dto)
    {
        if ((dto.IdCategoria ?? 0) > 0)
            return;
        if (string.IsNullOrWhiteSpace(dto.NombreCategoria))
            return;

        dto.IdCategoria = await Repository.FindCategoriaIdByNombre(dto.NombreCategoria);
    }
}
