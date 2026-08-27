using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public sealed class PrestamoEstadoRepository
{
    public const int BatchSize = 200;
    private readonly ApplicationDbContext _dbContext;

    public PrestamoEstadoRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<(int CodigoImt, string? NombreGrupoEquipo, string EstadoEquipo)>> ApplyEstadoEquipoRetorno(
        int prestamoId,
        Dictionary<int, EstadoEquipo> statesByCode,
        CancellationToken cancellationToken = default
    )
    {
        var details = await _dbContext
            .DetallesPrestamos.Where(detail =>
                detail.IdPrestamo == prestamoId && detail.IdEquipo != null
            )
            .ToListAsync(cancellationToken);

        var equipmentIds = details.Select(detail => detail.IdEquipo!.Value).ToHashSet();
        var equipment = await _dbContext
            .Equipos.Where(item => equipmentIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var groupIds = equipment.Select(item => item.IdGrupoEquipo).ToHashSet();
        var groups = await _dbContext
            .GruposEquipos.Where(group => groupIds.Contains(group.Id))
            .ToDictionaryAsync(group => group.Id, group => group.Nombre, cancellationToken);
        var detailsByEquipment = details.ToDictionary(detail => detail.IdEquipo!.Value);
        var result = new List<(int, string?, string)>();

        foreach (var item in equipment)
        {
            if (!statesByCode.TryGetValue(item.CodigoImt, out var state))
                continue;

            item.EstadoEquipo = state;

            if (detailsByEquipment.TryGetValue(item.Id, out var detail))
                detail.EstadoEquipoRetorno = state;

            groups.TryGetValue(item.IdGrupoEquipo, out var name);
            result.Add((item.CodigoImt, name, ToPostgresEstadoEquipo(state)));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

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

        var availableQuantity = await _dbContext
            .Equipos.Where(equipment =>
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
            )
            .CountAsync(cancellationToken);

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

        return availableQuantity - unassignedReservations >= requiredQuantity;
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

        var loanedIds = await _dbContext
            .DetallesPrestamos.Join(
                _dbContext.Prestamos,
                detail => detail.IdPrestamo,
                item => item.Id,
                (detail, item) => new { Detail = detail, Loan = item }
            )
            .Where(activeLoan =>
                activeLoan.Detail.IdEquipo != null
                && activeLoan.Loan.Id != prestamoId
                && (
                    activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Aprobado
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Activo
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Atrasado
                )
                && activeLoan.Loan.FechaPrestamoEsperada < loan.FechaDevolucionEsperada
                && activeLoan.Loan.FechaDevolucionEsperada > loan.FechaPrestamoEsperada
            )
            .Select(activeLoan => activeLoan.Detail.IdEquipo!.Value)
            .ToListAsync(cancellationToken);

        var requiredGroups = details.Select(detail => detail.IdGrupoEquipo).ToHashSet();
        var candidatesByGroup = (
            await _dbContext
                .Equipos.Where(equipment =>
                    requiredGroups.Contains(equipment.IdGrupoEquipo)
                    && !equipment.EstadoEliminado
                    && equipment.EstadoEquipo == EstadoEquipo.Operativo
                    && !loanedIds.Contains(equipment.Id)
                    && !_dbContext
                        .DetallesMantenimientos.Join(
                            _dbContext.Mantenimientos,
                            detail => detail.IdMantenimiento,
                            maintenance => maintenance.Id,
                            (detail, maintenance) => new { Detail = detail, Maintenance = maintenance }
                        )
                        .Any(activeMaintenance =>
                            activeMaintenance.Detail.IdEquipo == equipment.Id
                            && activeMaintenance.Maintenance.FechaMantenimiento
                                < loan.FechaDevolucionEsperada
                            && activeMaintenance.Maintenance.FechaFinalMantenimiento
                                > loan.FechaPrestamoEsperada
                        )
                )
                .OrderBy(equipment => equipment.Id)
                .Select(equipment => new { equipment.Id, equipment.IdGrupoEquipo })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(equipment => equipment.IdGrupoEquipo)
            .ToDictionary(
                group => group.Key,
                group => new Queue<int>(group.Select(equipment => equipment.Id))
            );

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

    public Task<List<PrestamoDto>> GetOverdueLoans(
        DateTime now,
        CancellationToken cancellationToken = default
    ) => GetBatch(
        _dbContext.Prestamos.Where(loan =>
            loan.EstadoPrestamo == EstadoPrestamo.Activo
            && loan.FechaDevolucionEsperada < now
            && !loan.EstadoEliminado
        ),
        cancellationToken
    );

    public Task<List<PrestamoDto>> GetExpiredPendingLoans(
        DateTime now,
        CancellationToken cancellationToken = default
    ) => GetBatch(
        _dbContext.Prestamos.Where(loan =>
            (
                loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                || loan.EstadoPrestamo == EstadoPrestamo.Aprobado
            )
            && loan.FechaPrestamoEsperada < now
            && !loan.EstadoEliminado
        ),
        cancellationToken
    );

    public Task<List<PrestamoDto>> GetLoansDueForReminder(
        DateTime now,
        DateTime reminderDeadline,
        CancellationToken cancellationToken = default
    ) => GetBatch(
        _dbContext.Prestamos.Where(loan =>
            loan.EstadoPrestamo == EstadoPrestamo.Activo
            && loan.FechaDevolucionEsperada > now
            && loan.FechaDevolucionEsperada <= reminderDeadline
            && !loan.RecordatorioEnviado
            && !loan.EstadoEliminado
        ),
        cancellationToken
    );

    public Task MarkAsOverdue(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default
    ) => UpdateStatus(ids, EstadoPrestamo.Atrasado, cancellationToken);

    public Task MarkAsRejected(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default
    ) => UpdateStatus(ids, EstadoPrestamo.Rechazado, cancellationToken);

    public async Task MarkReminderSent(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default
    )
    {
        if (ids.Count == 0)
            return;

        await _dbContext
            .Prestamos.Where(loan => ids.Contains(loan.Id))
            .ExecuteUpdateAsync(
                update => update.SetProperty(loan => loan.RecordatorioEnviado, true),
                cancellationToken
            );
    }

    private async Task UpdateStatus(
        IReadOnlyCollection<int> ids,
        EstadoPrestamo estado,
        CancellationToken cancellationToken
    )
    {
        if (ids.Count == 0)
            return;

        await _dbContext
            .Prestamos.Where(loan => ids.Contains(loan.Id))
            .ExecuteUpdateAsync(
                update => update.SetProperty(loan => loan.EstadoPrestamo, estado),
                cancellationToken
            );
    }

    private static Task<List<PrestamoDto>> GetBatch(
        IQueryable<PrestamoEntity> query,
        CancellationToken cancellationToken
    ) => query
        .AsNoTracking()
        .OrderBy(loan => loan.FechaDevolucionEsperada)
        .ThenBy(loan => loan.Id)
        .Take(BatchSize)
        .Select(loan => new PrestamoDto
        {
            Id = loan.Id,
            CarnetUsuario = loan.Carnet ?? string.Empty,
            FechaDevolucionEsperada = loan.FechaDevolucionEsperada,
        })
        .ToListAsync(cancellationToken);

    private static string ToPostgresEstadoEquipo(EstadoEquipo state) => state switch
    {
        EstadoEquipo.ParcialmenteOperativo => "parcialmente_operativo",
        EstadoEquipo.Inoperativo => "inoperativo",
        _ => "operativo",
    };
}
