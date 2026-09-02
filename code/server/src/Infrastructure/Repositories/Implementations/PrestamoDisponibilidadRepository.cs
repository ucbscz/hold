using System.Data;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

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
        grupoEquipoId,
        1,
        startDate,
        endDate,
        cancellationToken
    );

    public async Task<bool> HasAvailableEquipos(
        int grupoEquipoId,
        int requiredQuantity,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        if (requiredQuantity <= 0 || endDate <= startDate)
            return false;

        var availableQuantity = await AvailableEquipos(
                grupoEquipoId,
                startDate,
                endDate
            )
            .Take(requiredQuantity)
            .CountAsync(cancellationToken);

        return availableQuantity >= requiredQuantity;
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
                        required.Key,
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
        int grupoEquipoId,
        DateTime startDate,
        DateTime endDate
    ) => _dbContext.Equipos.Where(equipment =>
        equipment.IdGrupoEquipo == grupoEquipoId
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
