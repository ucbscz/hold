using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using EquipoEntity = IMT_Reservas.Server.Core.Entities.Equipo;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class EquipoRepository : Repository<EquipoEntity, EquipoDto>
{
    public EquipoRepository(ApplicationDbContext dbContext, EquipoMapper mapper)
        : base(dbContext, mapper) { }

    public async Task<int> GetMaxCodigoImt() =>
        await DbContext.Equipos.MaxAsync(e => (int?)e.CodigoImt) ?? 0;

    public async Task<bool> ExistsByCodigoImt(int codigoImt, int? excludeId = null) =>
        await DbContext.Equipos.AnyAsync(e =>
            e.CodigoImt == codigoImt && !e.EstadoEliminado && e.Id != excludeId
        );

    public async Task<bool> ExistsByCodigoUcb(string codigoUcb, int? excludeId = null) =>
        await DbContext
            .Equipos.IgnoreQueryFilters()
            .AnyAsync(e => e.CodigoUcb == codigoUcb && e.Id != excludeId);

    public async Task<EquipoEntity?> FindById(int id) =>
        await DbContext
            .Equipos.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && !e.EstadoEliminado);

    public async Task RecalcGrupoStats(int idGrupoEquipo)
    {
        var group = await DbContext.GruposEquipos.FirstOrDefaultAsync(group =>
            group.Id == idGrupoEquipo
        );

        if (group == null)
            return;

        var stats = await DbContext
            .Equipos.Where(e => e.IdGrupoEquipo == idGrupoEquipo && !e.EstadoEliminado)
            .GroupBy(e => e.IdGrupoEquipo)
            .Select(group => new
            {
                Cantidad = group.Count(),
                CostoTotal = group.Sum(e => e.CostoReferencia ?? 0),
            })
            .FirstOrDefaultAsync();

        group.Cantidad = stats?.Cantidad ?? 0;
        group.CostoPromedio =
            stats == null || stats.Cantidad == 0
                ? 0
                : (decimal)(stats.CostoTotal / stats.Cantidad);

        await DbContext.SaveChangesAsync();
    }

    public async Task<List<EquipoDto>> GetByGrupo(int grupoId) =>
        await ProjectTo(
                DbContext.Equipos.Where(e => e.IdGrupoEquipo == grupoId && !e.EstadoEliminado)
            )
            .ToListAsync();

    public async Task<List<EquipoDto>> GetByGavetero(int gaveteroId) =>
        await ProjectTo(
                DbContext.Equipos.Where(e => e.IdGavetero == gaveteroId && !e.EstadoEliminado)
            )
            .ToListAsync();

    public async Task<List<HistorialEquipoDto>> GetHistorial(int equipoId)
    {
        var rows = await DbContext
            .DetallesPrestamos.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => d.IdEquipo == equipoId)
            .Join(
                DbContext.Prestamos.IgnoreQueryFilters(),
                d => d.IdPrestamo,
                p => p.Id,
                (d, p) => new { d, p }
            )
            .Join(
                DbContext.Usuarios.IgnoreQueryFilters(),
                x => x.p.Carnet,
                u => u.Carnet,
                (x, u) =>
                    new
                    {
                        IdPrestamo = x.p.Id,
                        IdDetalle = x.d.Id,
                        u.Carnet,
                        u.Nombre,
                        u.ApellidoPaterno,
                        x.p.FechaPrestamo,
                        x.p.FechaDevolucionEsperada,
                        x.p.FechaDevolucion,
                        x.p.EstadoPrestamo,
                        x.d.EstadoEquipoRetorno,
                        x.p.Observacion,
                    }
            )
            .OrderByDescending(r => r.FechaPrestamo)
            .ToListAsync();

        return rows.GroupBy(r => r.IdPrestamo)
            .Select(group => group.OrderByDescending(r => r.IdDetalle).First())
            .Select(r => new HistorialEquipoDto
            {
                IdPrestamo = r.IdPrestamo,
                Carnet = r.Carnet,
                NombreUsuario = r.Nombre + " " + r.ApellidoPaterno,
                FechaPrestamo = r.FechaPrestamo,
                FechaDevolucionEsperada = r.FechaDevolucionEsperada,
                FechaDevolucion = r.FechaDevolucion,
                EstadoPrestamo = r.EstadoPrestamo.ToString().ToLowerInvariant(),
                EstadoEquipo = r.EstadoEquipoRetorno.HasValue
                        ? ToPostgresEstadoEquipo(r.EstadoEquipoRetorno.Value)
                        : null,
                Observacion = r.Observacion,
            })
            .ToList();
    }

    private static string ToPostgresEstadoEquipo(EstadoEquipo state) =>
        state switch
        {
            EstadoEquipo.ParcialmenteOperativo => "parcialmente_operativo",
            EstadoEquipo.Inoperativo => "inoperativo",
            EstadoEquipo.EnMantenimiento => "en_mantenimiento",
            _ => "operativo",
        };

    protected override async Task CascadeDelete(EquipoEntity equipment)
    {
        await CascadeLeaf<Accesorio>(accessory => accessory.IdEquipo == equipment.Id);
        await CascadeLeaf<Componente>(component => component.IdEquipo == equipment.Id);
        await CascadeLeaf<DetalleMantenimiento>(detail => detail.IdEquipo == equipment.Id);
    }
}
