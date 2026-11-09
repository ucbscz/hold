using System.Globalization;
using Ardalis.Result;
using FluentValidation;

namespace UCB_Hold.Server.Shared.Kernel;

public class Service<TEntity, TRepository, TDto>
    where TEntity : Entity
    where TRepository : Repository<TEntity, TDto>
    where TDto : class
{
    private readonly IMapper<TEntity, TDto> _mapper;
    private readonly IAuditTrail _audit;

    protected TRepository Repository { get; }
    protected IValidator<TDto> Validator { get; }

    public Service(
        TRepository repository,
        IValidator<TDto> validator,
        IMapper<TEntity, TDto> mapper,
        IAuditTrail audit
    )
    {
        Repository = repository;
        Validator = validator;
        _mapper = mapper;
        _audit = audit;
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
            await _audit.Created(typeof(TEntity).Name, id, result.Value);
        }

        return result;
    }

    public virtual async Task<Result<TDto>> Update(int id, TDto dto)
    {
        var previous = await Repository.Get(id);
        var result = await ValidateAndUpdate(id, dto);

        if (result.IsSuccess)
            await _audit.Updated(
                typeof(TEntity).Name,
                id.ToString(CultureInfo.InvariantCulture),
                previous.IsSuccess ? previous.Value : null,
                result.Value
            );

        return result;
    }

    public virtual async Task<Result<object>> Delete(int id)
    {
        var previous = await Repository.Get(id);
        var result = await Repository.Delete(id);

        if (result.IsSuccess)
            await _audit.Deleted(
                typeof(TEntity).Name,
                id.ToString(CultureInfo.InvariantCulture),
                previous.IsSuccess ? previous.Value : null
            );

        return result;
    }

    public virtual async Task<Result<TDto>> Get(int id) => await Repository.Get(id);

    public virtual async Task<Result<List<TDto>>> GetAll() => await Repository.GetAll();

    public virtual Task<Result<List<TDto>>> GetPage(int page, int pageSize) =>
        Repository.GetPage(page, pageSize);

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
