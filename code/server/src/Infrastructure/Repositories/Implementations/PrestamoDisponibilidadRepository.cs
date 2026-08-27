using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

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

        var unassignedReservations = await _dbContext
            .DetallesPrestamos.Join(
                _dbContext.Prestamos,
                detail => detail.IdPrestamo,
                loan => loan.Id,
                (detail, loan) => new { Detail = detail, Loan = loan }
            )
            .CountAsync(
                reservation =>
                    reservation.Detail.IdGrupoEquipo == grupoEquipoId
                    && reservation.Detail.IdEquipo == null
                    && reservation.Loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                    && reservation.Loan.FechaPrestamoEsperada < endDate
                    && reservation.Loan.FechaDevolucionEsperada > startDate,
                cancellationToken
            );

        var requiredAvailableQuantity = requiredQuantity + unassignedReservations;
        var availableQuantity = await AvailableEquipos(
                grupoEquipoId,
                startDate,
                endDate
            )
            .Take(requiredAvailableQuantity)
            .CountAsync(cancellationToken);

        return availableQuantity >= requiredAvailableQuantity;
    }

    public async Task<bool> AssignEquiposOnApproval(
        int prestamoId,
        CancellationToken cancellationToken = default
    )
    {
        var loan = await _dbContext.Prestamos.FirstOrDefaultAsync(
            item => item.Id == prestamoId,
            cancellationToken
        );

        if (loan == null)
            return false;

        var details = await _dbContext
            .DetallesPrestamos.Where(detail =>
                detail.IdPrestamo == prestamoId
                && !detail.EstadoEliminado
                && detail.IdEquipo == null
            )
            .ToListAsync(cancellationToken);

        if (details.Count == 0)
            return true;

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

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
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
                && (
                    activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Aprobado
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Activo
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Atrasado
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
