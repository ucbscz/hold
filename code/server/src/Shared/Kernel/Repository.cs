using System.Linq.Expressions;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Core.Abstraction;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;

public class Repository<TEntity, TDto>
    where TEntity : Entity
    where TDto : class
{
    protected readonly ApplicationDbContext DbContext;
    private readonly IMapper<TEntity, TDto> _mapper;

    public Repository(ApplicationDbContext dbContext, IMapper<TEntity, TDto> mapper)
    {
        DbContext = dbContext;
        _mapper = mapper;
    }

    protected TDto MapToDto(TEntity entity) => _mapper.ToDto(entity);

    protected IQueryable<TDto> ProjectTo(IQueryable<TEntity> source) => _mapper.ProjectTo(source);

    public virtual async Task<Result<TDto>> Create(TEntity entity)
    {
        entity.EstadoEliminado = false;
        DbContext.Add(entity);
        await DbContext.SaveChangesAsync();
        return Result<TDto>.Created(MapToDto(entity));
    }

    public virtual async Task<Result<TDto>> Update(TEntity entity)
    {
        var existing = await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == entity.Id);

        if (existing == null)
            return Result<TDto>.NotFound();

        DbContext.Entry(existing).CurrentValues.SetValues(entity);
        existing.EstadoEliminado = false;
        await DbContext.SaveChangesAsync();
        return Result<TDto>.Success(MapToDto(existing));
    }

    public virtual async Task<Result<object>> Delete(int id)
    {
        var entity = await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
            return Result<object>.NotFound();

        await SoftDelete(entity);
        await DbContext.SaveChangesAsync();

        return Result<object>.Success(null!);
    }

    internal async Task SoftDelete(TEntity entity)
    {
        entity.EstadoEliminado = true;
        await CascadeDelete(entity);
    }

    protected async Task CascadeThrough<TChild, TChildDto>(
        Repository<TChild, TChildDto> childRepository,
        Expression<Func<TChild, bool>> dependents
    )
        where TChild : Entity
        where TChildDto : class
    {
        var children = await DbContext.Set<TChild>().Where(dependents).ToListAsync();

        foreach (var child in children)
            await childRepository.SoftDelete(child);
    }

    protected async Task CascadeLeaf<TChild>(Expression<Func<TChild, bool>> dependents)
        where TChild : Entity
    {
        var children = await DbContext.Set<TChild>().Where(dependents).ToListAsync();

        foreach (var child in children)
            child.EstadoEliminado = true;
    }

    protected virtual Task CascadeDelete(TEntity entity) => Task.CompletedTask;

    public virtual async Task<Result<TDto>> Get(int id)
    {
        var dto = await _mapper
            .ProjectTo(DbContext.Set<TEntity>().AsNoTracking().Where(e => e.Id == id))
            .FirstOrDefaultAsync();

        return dto == null ? Result<TDto>.NotFound() : Result<TDto>.Success(dto);
    }

    public virtual async Task<Result<List<TDto>>> GetAll()
    {
        var dtos = await _mapper.ProjectTo(DbContext.Set<TEntity>().AsNoTracking()).ToListAsync();
        return Result<List<TDto>>.Success(dtos);
    }
}
