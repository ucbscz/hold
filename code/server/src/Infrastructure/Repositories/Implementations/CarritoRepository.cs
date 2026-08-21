using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class CarritoRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CarritoRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Dictionary<int, int>> GetCantidadesByGrupos(List<int> grupoIds)
    {
        if (grupoIds.Count == 0)
            return [];

        return await _dbContext
            .Equipos.AsNoTracking()
            .Where(e =>
                grupoIds.Contains(e.IdGrupoEquipo)
                && !e.EstadoEliminado
                && e.EstadoEquipo == EstadoEquipo.Operativo
            )
            .GroupBy(e => e.IdGrupoEquipo)
            .Select(group => new { GrupoId = group.Key, Cantidad = group.Count() })
            .ToDictionaryAsync(group => group.GrupoId, group => group.Cantidad);
    }

    public async Task<Dictionary<int, (string Nombre, int MaximoDias)>> GetLoanLimitsByGroups(
        IReadOnlyCollection<int> grupoIds
    ) =>
        await _dbContext
            .GruposEquipos.AsNoTracking()
            .Where(group => grupoIds.Contains(group.Id))
            .ToDictionaryAsync(
                group => group.Id,
                group => new ValueTuple<string, int>(
                    group.Nombre,
                    group.TiempoMaximoPrestamoDias
                )
            );

    public async Task<
        List<(int IdGrupoEquipo, int IdEquipo)>
    > GetPrestamosActivosEnRango(List<int> grupoIds, DateTime fechaInicio, DateTime fechaFin)
    {
        var rows = await (
            from detalle in _dbContext.DetallesPrestamos.AsNoTracking()
            join prestamo in _dbContext.Prestamos.AsNoTracking()
                on detalle.IdPrestamo equals prestamo.Id
            join equipo in _dbContext.Equipos.AsNoTracking() on detalle.IdEquipo equals equipo.Id
            where
                grupoIds.Contains(equipo.IdGrupoEquipo)
                && (
                    prestamo.EstadoPrestamo == EstadoPrestamo.Pendiente
                    || prestamo.EstadoPrestamo == EstadoPrestamo.Activo
                    || prestamo.EstadoPrestamo == EstadoPrestamo.Aprobado
                    || prestamo.EstadoPrestamo == EstadoPrestamo.Atrasado
                )
                && prestamo.FechaPrestamoEsperada < fechaFin
                && prestamo.FechaDevolucionEsperada > fechaInicio
            select new
            {
                equipo.IdGrupoEquipo,
                equipo.Id,
                prestamo.FechaPrestamoEsperada,
                prestamo.FechaDevolucionEsperada,
            }
        ).ToListAsync();

        return rows.ConvertAll(r =>
            (r.IdGrupoEquipo, r.Id)
        );
    }

    public async Task<
        List<(int IdGrupoEquipo, int IdEquipo)>
    > GetMantenimientosActivosEnRango(List<int> grupoIds, DateTime fechaInicio, DateTime fechaFin)
    {
        var rows = await (
            from detalle in _dbContext.DetallesMantenimientos.AsNoTracking()
            join mantenimiento in _dbContext.Mantenimientos.AsNoTracking()
                on detalle.IdMantenimiento equals mantenimiento.Id
            join equipo in _dbContext.Equipos.AsNoTracking() on detalle.IdEquipo equals equipo.Id
            where
                grupoIds.Contains(equipo.IdGrupoEquipo)
                && mantenimiento.FechaMantenimiento < fechaFin
                && mantenimiento.FechaFinalMantenimiento > fechaInicio
            select new
            {
                equipo.IdGrupoEquipo,
                equipo.Id,
                mantenimiento.FechaMantenimiento,
                mantenimiento.FechaFinalMantenimiento,
            }
        ).ToListAsync();

        return rows.ConvertAll(r =>
            (r.IdGrupoEquipo, r.Id)
        );
    }
}
