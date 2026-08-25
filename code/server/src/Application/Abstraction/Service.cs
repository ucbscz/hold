using System.Globalization;
using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Application.Abstraction;

public class Service<TEntity, TRepository, TDto>
    where TEntity : Entity
    where TRepository : IRepository<TEntity, TDto>
    where TDto : class
{
    private readonly IMapper<TEntity, TDto> _mapper;

    protected TRepository Repository { get; }
    protected IValidator<TDto> Validator { get; }
    protected AuditLogService Audit { get; }

    public Service(
        TRepository repository,
        IValidator<TDto> validator,
        IMapper<TEntity, TDto> mapper,
        AuditLogService audit
    )
    {
        Repository = repository;
        Validator = validator;
        _mapper = mapper;
        Audit = audit;
    }

    protected TEntity MapToEntity(TDto dto) => _mapper.ToEntity(dto);

    protected TDto MapToDto(TEntity entity) => _mapper.ToDto(entity);

    protected async Task<Result<TDto>> CreateEntity(TEntity entity) =>
        await Repository.Create(entity);

    protected async Task<Result<TDto>> UpdateEntity(TEntity entity) =>
        await Repository.Update(entity);

    public virtual async Task<Result<TDto>> Create(TDto dto)
    {
        var result = await ValidateAndCreate(dto);

        if (result.IsSuccess)
        {
            var id = Convert.ToString(
                typeof(TDto).GetProperty("Id")?.GetValue(result.Value),
                CultureInfo.InvariantCulture
            );
            await Audit.Log(AuditAccion.Crear, typeof(TEntity).Name, id);
        }

        return result;
    }

    public virtual async Task<Result<TDto>> Update(int id, TDto dto)
    {
        var result = await ValidateAndUpdate(id, dto);

        if (result.IsSuccess)
            await Audit.Log(
                AuditAccion.Editar,
                typeof(TEntity).Name,
                id.ToString(CultureInfo.InvariantCulture)
            );

        return result;
    }

    public virtual async Task<Result<object>> Delete(int id)
    {
        var result = await Repository.Delete(id);

        if (result.IsSuccess)
            await Audit.Log(
                AuditAccion.Eliminar,
                typeof(TEntity).Name,
                id.ToString(CultureInfo.InvariantCulture)
            );

        return result;
    }

    public virtual async Task<Result<TDto>> Get(int id) => await Repository.Get(id);

    public virtual async Task<Result<List<TDto>>> GetAll() => await Repository.GetAll();

    private async Task<Result<TDto>> ValidateAndCreate(TDto dto)
    {
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<TDto>();

        return await CreateEntity(MapToEntity(dto));
    }

    private async Task<Result<TDto>> ValidateAndUpdate(int id, TDto dto)
    {
        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<TDto>();

        var entity = MapToEntity(dto);
        entity.Id = id;

        return await UpdateEntity(entity);
    }
}
