using Ardalis.Result;
using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Application.Abstraction;

public interface IRepository<TEntity, TDto>
    where TEntity : Entity
    where TDto : class
{
    Task<Result<TDto>> Create(TEntity entity);
    Task<Result<TDto>> Update(TEntity entity);
    Task<Result<object>> Delete(int id);
    Task<Result<TDto>> Get(int id);
    Task<Result<List<TDto>>> GetAll();
}
