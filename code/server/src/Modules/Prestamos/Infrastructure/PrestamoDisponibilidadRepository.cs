using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Prestamos.Infrastructure;

public sealed class PrestamoDisponibilidadRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PrestamoDisponibilidadRepository(ApplicationDbContext dbContext) =>
        _dbContext = dbContext;

    public Task<bool> HasAvailableEquipo(
        int grupoEquipoId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    ) => HasAvailableEquipos(
        new Dictionary<int, int> { [grupoEquipoId] = 1 },
        startDate,
        endDate,
        cancellationToken
    );

    public async Task<bool> HasAvailableEquipos(
        IReadOnlyDictionary<int, int> requiredByGroup,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        if (requiredByGroup.Values.Any(quantity => quantity <= 0) || endDate <= startDate)
            return false;
        if (requiredByGroup.Count == 0)
            return true;

        var availableByGroup = await AvailableEquipos(
                requiredByGroup.Keys.ToArray(),
                startDate,
                endDate
            )
            .GroupBy(equipment => equipment.IdGrupoEquipo)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

        return requiredByGroup.All(required =>
            availableByGroup.GetValueOrDefault(required.Key) >= required.Value
        );
    }

    public async Task<bool> AssignEquiposOnApproval(
        int prestamoId,
        string actor,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken
            )
            : null;

        try
        {
            var loan = await _dbContext.Prestamos.FirstOrDefaultAsync(
                item => item.Id == prestamoId,
                cancellationToken
            );

            if (loan == null || loan.EstadoPrestamo != EstadoPrestamo.Pendiente)
                return false;

            var details = await _dbContext
                .DetallesPrestamos.Where(detail =>
                    detail.IdPrestamo == prestamoId && !detail.EstadoEliminado
                )
                .ToListAsync(cancellationToken);

            if (details.Count == 0)
                return false;

            var requiredByGroup = details
                .GroupBy(detail => detail.IdGrupoEquipo)
                .ToDictionary(group => group.Key, group => group.Count());
            var candidatesByGroup = new Dictionary<int, Queue<int>>();

            foreach (var required in requiredByGroup)
            {
                var candidateIds = await AvailableEquipos(
                        [required.Key],
                        loan.FechaPrestamoEsperada,
                        loan.FechaDevolucionEsperada
                    )
                    .OrderBy(equipment => equipment.Id)
                    .Select(equipment => equipment.Id)
                    .Take(required.Value)
                    .ToListAsync(cancellationToken);

                if (candidateIds.Count < required.Value)
                    return false;

                candidatesByGroup[required.Key] = new Queue<int>(candidateIds);
            }

            foreach (var detail in details)
            {
                if (
                    !candidatesByGroup.TryGetValue(detail.IdGrupoEquipo, out var available)
                    || available.Count == 0
                )
                    return false;

                detail.IdEquipo = available.Dequeue();
            }

            loan.EstadoPrestamo = EstadoPrestamo.Aprobado;
            loan.AutorizadoPor = actor;
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.SerializationFailure,
            })
        {
            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);
            return false;
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    private IQueryable<Equipo> AvailableEquipos(
        int[] grupoEquipoIds,
        DateTime startDate,
        DateTime endDate
    ) => _dbContext.Equipos.Where(equipment =>
        grupoEquipoIds.Contains(equipment.IdGrupoEquipo)
        && !equipment.EstadoEliminado
        && equipment.EstadoEquipo == EstadoEquipo.Operativo
        && !_dbContext
            .DetallesPrestamos.Join(
                _dbContext.Prestamos,
                detail => detail.IdPrestamo,
                loan => loan.Id,
                (detail, loan) => new { Detail = detail, Loan = loan }
            )
            .Any(activeLoan =>
                activeLoan.Detail.IdEquipo == equipment.Id
                && PrestamoAvailabilityPolicy.BlockingStates.Contains(
                    activeLoan.Loan.EstadoPrestamo
                )
                && activeLoan.Loan.FechaPrestamoEsperada < endDate
                && activeLoan.Loan.FechaDevolucionEsperada > startDate
            )
        && !_dbContext
            .DetallesMantenimientos.Join(
                _dbContext.Mantenimientos,
                detail => detail.IdMantenimiento,
                maintenance => maintenance.Id,
                (detail, maintenance) => new { Detail = detail, Maintenance = maintenance }
            )
            .Any(activeMaintenance =>
                activeMaintenance.Detail.IdEquipo == equipment.Id
                && activeMaintenance.Maintenance.FechaMantenimiento < endDate
                && activeMaintenance.Maintenance.FechaFinalMantenimiento > startDate
            )
    );
}
