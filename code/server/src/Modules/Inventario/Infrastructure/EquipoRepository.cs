using Microsoft.EntityFrameworkCore;
using EquipoEntity = UCB_Hold.Server.Modules.Inventario.Domain.Equipo;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Inventario.Infrastructure;

public class EquipoRepository : Repository<EquipoEntity, EquipoDto>
{
    private readonly IReadOnlyList<IEquipoEliminacion> _eliminaciones;

    public EquipoRepository(
        ApplicationDbContext dbContext,
        EquipoMapper mapper,
        IEnumerable<IEquipoEliminacion> eliminaciones
    )
        : base(dbContext, mapper) => _eliminaciones = eliminaciones.ToList();

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

    protected override async Task CascadeDelete(EquipoEntity equipment)
    {
        await CascadeLeaf<Accesorio>(accessory => accessory.IdEquipo == equipment.Id);
        await CascadeLeaf<Componente>(component => component.IdEquipo == equipment.Id);
        foreach (var eliminacion in _eliminaciones)
            await eliminacion.AlEliminarEquipo(equipment.Id);
    }
}
