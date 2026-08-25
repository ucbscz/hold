namespace IMT_Reservas.Server.Application.Abstraction;

public interface IUpdateMapper<TEntity, TDto> : IMapper<TEntity, TDto>
{
    void UpdateEntity(TDto source, TEntity destination);
}
