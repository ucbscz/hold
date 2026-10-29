namespace UCB_Hold.Server.Shared.Kernel;

public interface IMapper<TEntity, TDto>
{
    TDto ToDto(TEntity entity);
    TEntity ToEntity(TDto dto);
    IQueryable<TDto> ProjectTo(IQueryable<TEntity> source);
}
