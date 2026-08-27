using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using UsuarioEntity = IMT_Reservas.Server.Core.Entities.Usuario;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class UsuarioRepository : Repository<UsuarioEntity, UsuarioDto>
{
    private readonly UsuarioConsultaRepository _queries;

    public UsuarioRepository(
        ApplicationDbContext dbContext,
        UsuarioMapper mapper,
        UsuarioConsultaRepository queries
    )
        : base(dbContext, mapper) => _queries = queries;

    public override Task<Result<List<UsuarioDto>>> GetAll() => _queries.GetAll();

    public Task<Result<List<UsuarioDto>>> GetAll(CancellationToken cancellationToken) =>
        _queries.GetAll(cancellationToken);

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

    public async Task<Result<object>> Delete(string carnet)
    {
        var entity = await DbContext.Usuarios.FirstOrDefaultAsync(user =>
            user.Carnet == carnet && !user.EstadoEliminado
        );

        if (entity == null)
            return Result<object>.NotFound();

        await SoftDelete(entity);
        await DbContext.SaveChangesAsync();
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
        var loans = await DbContext
            .Prestamos.Where(loan => loan.Carnet == user.Carnet)
            .ToListAsync();
        var loanIds = loans.Select(loan => loan.Id).ToArray();
        var details = await DbContext
            .DetallesPrestamos.Where(detail => loanIds.Contains(detail.IdPrestamo))
            .ToListAsync();

        foreach (var loan in loans)
        {
            loan.EstadoPrestamo = EstadoPrestamo.Cancelado;
            loan.EstadoEliminado = true;
        }

        foreach (var detail in details)
            detail.EstadoEliminado = true;
    }
}
