using Ardalis.Result;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Inventario;
using IMT_Reservas.Server.Core.Abstraction;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public sealed class CatalogoInventarioRepository<TEntity>(ApplicationDbContext db, IMapper<TEntity, CatalogoInventarioDto> mapper)
    : Repository<TEntity, CatalogoInventarioDto>(db, mapper) where TEntity : Entity
{
    public override async Task<Result<CatalogoInventarioDto>> Create(TEntity entity) =>
        await Exists(entity) ? Result<CatalogoInventarioDto>.Conflict("Ya existe un registro con ese nombre") : await base.Create(entity);

    public override async Task<Result<CatalogoInventarioDto>> Update(TEntity entity) =>
        await Exists(entity) ? Result<CatalogoInventarioDto>.Conflict("Ya existe un registro con ese nombre") : await base.Update(entity);

    private Task<bool> Exists(TEntity entity)
    {
        var name = entity switch { Ambiente a => a.Nombre = a.Nombre.Trim(), Procedencia p => p.Nombre = p.Nombre.Trim(), _ => throw new InvalidOperationException() };
        return DbContext.Set<TEntity>().IgnoreQueryFilters().AnyAsync(e => e.Id != entity.Id && EF.Property<string>(e, "Nombre") == name);
    }

    public override async Task<Result<object>> Delete(int id)
    {
        var inUse = typeof(TEntity) == typeof(Ambiente)
            ? await DbContext.Equipos.IgnoreQueryFilters().AnyAsync(e => e.IdAmbiente == id)
            : await DbContext.Equipos.IgnoreQueryFilters().AnyAsync(e => e.IdProcedencia == id);
        return inUse ? Result<object>.Conflict("El registro está asignado a equipos; cambia su asignación antes de eliminarlo") : await base.Delete(id);
    }
}
