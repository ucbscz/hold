using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using UsuarioEntity = UCB_Hold.Server.Modules.Usuarios.Domain.Usuario;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Usuarios.Infrastructure;

public class UsuarioRepository : Repository<UsuarioEntity, UsuarioDto>, IUsuarioBloqueo
{
    private readonly UsuarioReadRepository _queries;
    private readonly IReadOnlyList<IUsuarioEliminacion> _eliminaciones;

    public UsuarioRepository(
        ApplicationDbContext dbContext,
        UsuarioMapper mapper,
        UsuarioReadRepository queries,
        IEnumerable<IUsuarioEliminacion> eliminaciones
    )
        : base(dbContext, mapper)
    {
        _queries = queries;
        _eliminaciones = eliminaciones.ToList();
    }

    public override Task<Result<List<UsuarioDto>>> GetAll() => _queries.GetAll();

    public Task<Result<List<UsuarioDto>>> GetPage(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    ) => _queries.GetPage(page, pageSize, cancellationToken);

    public Task<UsuarioEntity?> GetTrackedByCarnet(
        string carnet,
        CancellationToken cancellationToken = default
    ) => DbContext.Usuarios.FirstOrDefaultAsync(
        user => user.Carnet == carnet && !user.EstadoEliminado,
        cancellationToken
    );

    public Task<UsuarioEntity?> GetDeletedByCarnet(
        string carnet,
        CancellationToken cancellationToken = default
    ) => DbContext
        .Usuarios.IgnoreQueryFilters()
        .FirstOrDefaultAsync(user => user.Carnet == carnet && user.EstadoEliminado, cancellationToken);

    public async Task<Result<object>> Delete(
        string carnet,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await GetTrackedByCarnet(carnet, cancellationToken);

        if (entity == null)
            return Result<object>.NotFound();

        await SoftDelete(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
        return Result<object>.Success(null!);
    }

    public Task UpdateEntity(
        UsuarioEntity entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        DbContext.Usuarios.Update(entity);
        return saveChanges
            ? DbContext.SaveChangesAsync(cancellationToken)
            : Task.CompletedTask;
    }

    public Task SaveChanges(CancellationToken cancellationToken = default) =>
        DbContext.SaveChangesAsync(cancellationToken);

    public async Task SetBlockedStatus(
        IReadOnlyCollection<string> carnets,
        bool isBlocked,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        if (carnets.Count == 0)
            return;

        if (!DbContext.Database.IsRelational())
        {
            var users = await DbContext
                .Usuarios.Where(user => carnets.Contains(user.Carnet))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.Bloqueado = isBlocked;
                user.MotivoBloqueo = isBlocked ? reason : null;
            }

            await DbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await DbContext
            .Usuarios.Where(user => carnets.Contains(user.Carnet))
            .ExecuteUpdateAsync(
                update =>
                    update
                        .SetProperty(user => user.Bloqueado, isBlocked)
                        .SetProperty(user => user.MotivoBloqueo, isBlocked ? reason : null),
                cancellationToken
            );
    }

    protected override async Task CascadeDelete(UsuarioEntity user)
    {
        foreach (var eliminacion in _eliminaciones)
            await eliminacion.AlEliminarUsuario(user.Carnet);
    }
}
