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

        if (_dbContext.Database.IsRelational())
        {
            await _dbContext
                .Prestamos.Where(loan => ids.Contains(loan.Id))
                .ExecuteUpdateAsync(
                    update => update.SetProperty(loan => loan.EstadoPrestamo, estado),
                    cancellationToken
                );
            return;
        }

        var loans = await _dbContext
            .Prestamos.Where(loan => ids.Contains(loan.Id))
            .ToListAsync(cancellationToken);
        foreach (var loan in loans)
            loan.EstadoPrestamo = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<List<PrestamoDto>> GetBatch(
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
            FechaPrestamoEsperada = loan.FechaPrestamoEsperada,
            FechaDevolucionEsperada = loan.FechaDevolucionEsperada,
            NombreGrupoEquipo = _dbContext
                .DetallesPrestamos.Where(detail =>
                    detail.IdPrestamo == loan.Id && !detail.EstadoEliminado
                )
                .Join(
                    _dbContext.GruposEquipos,
                    detail => detail.IdGrupoEquipo,
                    group => group.Id,
                    (_, group) => group.Nombre
                )
                .FirstOrDefault(),
        })
        .ToListAsync(cancellationToken);

    private static string ToPostgresEstadoEquipo(EstadoEquipo state) => state switch
    {
        EstadoEquipo.ParcialmenteOperativo => "parcialmente_operativo",
        EstadoEquipo.Inoperativo => "inoperativo",
        EstadoEquipo.EnMantenimiento => "en_mantenimiento",
        _ => "operativo",
    };
}
