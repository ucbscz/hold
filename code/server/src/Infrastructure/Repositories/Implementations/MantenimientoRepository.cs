using System.Globalization;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Mantenimiento;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Core.Abstraction;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using MantenimientoEntity = IMT_Reservas.Server.Core.Entities.Mantenimiento;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class MantenimientoRepository : Repository<MantenimientoEntity, MantenimientoDto>
{
    public MantenimientoRepository(ApplicationDbContext dbContext, MantenimientoMapper mapper)
        : base(dbContext, mapper) { }

    public override async Task<Result<List<MantenimientoDto>>> GetAll()
    {
        var rows = await (
            from mantenimiento in DbContext.Mantenimientos.AsNoTracking()
            join empresa in DbContext.EmpresasMantenimiento.AsNoTracking()
                on mantenimiento.IdEmpresa equals empresa.Id
                into empresaJoin
            from empresa in empresaJoin.DefaultIfEmpty()
            join detalle in DbContext.DetallesMantenimientos.AsNoTracking()
                on mantenimiento.Id equals detalle.IdMantenimiento
                into detalleJoin
            from detalle in detalleJoin.DefaultIfEmpty()
            join equipo in DbContext.Equipos.AsNoTracking()
                on detalle.IdEquipo equals equipo.Id
                into equipoJoin
            from equipo in equipoJoin.DefaultIfEmpty()
            join grupo in DbContext.GruposEquipos.AsNoTracking()
                on equipo.IdGrupoEquipo equals grupo.Id
                into grupoJoin
            from grupo in grupoJoin.DefaultIfEmpty()
            select new
            {
                Mantenimiento = mantenimiento,
                NombreEmpresa = empresa != null ? empresa.Nombre : null,
                TipoMantenimiento = detalle != null ? detalle.TipoMantenimiento : null,
                CodigoImt = equipo != null ? (int?)equipo.CodigoImt : null,
                NombreGrupo = grupo != null ? grupo.Nombre : null,
                DescripcionEquipo = detalle != null ? detalle.Descripcion : null,
            }
        ).ToListAsync();

        var dtos = rows
            .Select(row => new MantenimientoDto
            {
                Id = row.Mantenimiento.Id,
                IdEmpresa = row.Mantenimiento.IdEmpresa,
                NombreEmpresaMantenimiento = row.NombreEmpresa,
                FechaMantenimiento = row.Mantenimiento.FechaMantenimiento,
                FechaFinalMantenimiento = row.Mantenimiento.FechaFinalMantenimiento,
                Costo = row.Mantenimiento.Costo,
                Descripcion = row.Mantenimiento.Descripcion,
                TipoMantenimiento = row.TipoMantenimiento,
                CodigoImtEquipo = row.CodigoImt?.ToString(CultureInfo.InvariantCulture),
                NombreGrupoEquipo = row.NombreGrupo,
                DescripcionEquipo = row.DescripcionEquipo,
            })
            .ToList();

        return Result<List<MantenimientoDto>>.Success(dtos);
    }

    public override async Task<Result<MantenimientoDto>> Get(int id)
    {
        var dto = await (
            from mantenimiento in DbContext.Mantenimientos.AsNoTracking()
            where mantenimiento.Id == id
            join empresa in DbContext.EmpresasMantenimiento.AsNoTracking()
                on mantenimiento.IdEmpresa equals empresa.Id
                into empresaJoin
            from empresa in empresaJoin.DefaultIfEmpty()
            select new MantenimientoDto
            {
                Id = mantenimiento.Id,
                IdEmpresa = mantenimiento.IdEmpresa,
                NombreEmpresaMantenimiento = empresa != null ? empresa.Nombre : null,
                FechaMantenimiento = mantenimiento.FechaMantenimiento,
                FechaFinalMantenimiento = mantenimiento.FechaFinalMantenimiento,
                Costo = mantenimiento.Costo,
                Descripcion = mantenimiento.Descripcion,
            }
        ).FirstOrDefaultAsync();

        return dto == null
            ? Result<MantenimientoDto>.NotFound()
            : Result<MantenimientoDto>.Success(dto);
    }

    protected override async Task CascadeDelete(MantenimientoEntity mantenimiento) =>
        await CascadeLeaf<DetalleMantenimiento>(d => d.IdMantenimiento == mantenimiento.Id);

    public async Task<bool> HasScheduleConflict(
        IReadOnlyCollection<int> codigosImt,
        DateTime start,
        DateTime end,
        int? excludedMaintenanceId = null
    )
    {
        if (codigosImt.Count == 0)
            return false;

        var equipmentIds = DbContext
            .Equipos.Where(equipment =>
                codigosImt.Contains(equipment.CodigoImt) && !equipment.EstadoEliminado
            )
            .Select(equipment => equipment.Id);

        var conflictsWithLoans = await DbContext
            .DetallesPrestamos.Join(
                DbContext.Prestamos,
                detail => detail.IdPrestamo,
                loan => loan.Id,
                (detail, loan) => new { Detail = detail, Loan = loan }
            )
            .AnyAsync(item =>
                item.Detail.IdEquipo != null
                && equipmentIds.Contains(item.Detail.IdEquipo.Value)
                && PrestamoAvailabilityPolicy.BlockingStates.Contains(
                    item.Loan.EstadoPrestamo
                )
                && item.Loan.FechaPrestamoEsperada < end
                && item.Loan.FechaDevolucionEsperada > start
            );

        if (conflictsWithLoans)
            return true;

        return await DbContext
            .DetallesMantenimientos.Join(
                DbContext.Mantenimientos,
                detail => detail.IdMantenimiento,
                maintenance => maintenance.Id,
                (detail, maintenance) => new { Detail = detail, Maintenance = maintenance }
            )
            .AnyAsync(item =>
                equipmentIds.Contains(item.Detail.IdEquipo)
                && item.Maintenance.Id != excludedMaintenanceId
                && item.Maintenance.FechaMantenimiento < end
                && item.Maintenance.FechaFinalMantenimiento > start
            );
    }

    public async Task AddDetalles(
        int mantenimientoId,
        int[] codigosImt,
        string[]? tipos,
        string[]? descripciones
    )
    {
        var equipoIdPorCodigo = await DbContext
            .Equipos.Where(e => codigosImt.Contains(e.CodigoImt) && !e.EstadoEliminado)
            .ToDictionaryAsync(e => e.CodigoImt, e => e.Id);

        for (var i = 0; i < codigosImt.Length; i++)
        {
            if (!equipoIdPorCodigo.TryGetValue(codigosImt[i], out var equipoId))
                continue;

            DbContext.DetallesMantenimientos.Add(
                new DetalleMantenimiento
                {
                    IdMantenimiento = mantenimientoId,
                    IdEquipo = equipoId,
                    TipoMantenimiento = tipos?.ElementAtOrDefault(i),
                    Descripcion = descripciones?.ElementAtOrDefault(i),
                    EstadoEliminado = false,
                }
            );
        }

        await DbContext.SaveChangesAsync();
    }

    public async Task ReplaceDetalles(
        int mantenimientoId,
        int[] codigosImt,
        string[]? tipos,
        string[]? descripciones
    )
    {
        var existing = DbContext.DetallesMantenimientos.Where(detail =>
            detail.IdMantenimiento == mantenimientoId
        );
        DbContext.DetallesMantenimientos.RemoveRange(existing);
        await DbContext.SaveChangesAsync();
        await AddDetalles(mantenimientoId, codigosImt, tipos, descripciones);
    }

    public async Task<List<(int Id, int CodigoImt, EstadoEquipo Estado)>> SyncEquipmentStates(
        DateTime now,
        CancellationToken cancellationToken = default
    )
    {
        var activeEquipmentIds = await (
            from detail in DbContext.DetallesMantenimientos.IgnoreQueryFilters()
            join maintenance in DbContext.Mantenimientos
                on detail.IdMantenimiento equals maintenance.Id
            where
                !detail.EstadoEliminado
                && !maintenance.EstadoEliminado
                && maintenance.FechaMantenimiento <= now
                && maintenance.FechaFinalMantenimiento > now
            select detail.IdEquipo
        ).Distinct().ToListAsync(cancellationToken);

        var changed = await DbContext
            .Equipos.Where(equipment =>
                !equipment.EstadoEliminado
                && (
                    (activeEquipmentIds.Contains(equipment.Id)
                        && equipment.EstadoEquipo == EstadoEquipo.Operativo)
                    || (!activeEquipmentIds.Contains(equipment.Id)
                        && equipment.EstadoEquipo == EstadoEquipo.EnMantenimiento)
                )
            )
            .Select(equipment => new
            {
                equipment.Id,
                equipment.CodigoImt,
                Active = activeEquipmentIds.Contains(equipment.Id),
            })
            .ToListAsync(cancellationToken);

        if (changed.Count == 0)
            return [];

        var changedIds = changed.Select(item => item.Id).ToArray();
        var entities = await DbContext
            .Equipos.Where(equipment => changedIds.Contains(equipment.Id))
            .ToListAsync(cancellationToken);

        foreach (var equipment in entities)
            equipment.EstadoEquipo = activeEquipmentIds.Contains(equipment.Id)
                ? EstadoEquipo.EnMantenimiento
                : EstadoEquipo.Operativo;

        await DbContext.SaveChangesAsync(cancellationToken);

        return changed
            .Select(item => (
                item.Id,
                item.CodigoImt,
                item.Active ? EstadoEquipo.EnMantenimiento : EstadoEquipo.Operativo
            ))
            .ToList();
    }
}
