using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Mantenimientos.Infrastructure;

public sealed class MantenimientosDeEquipoEliminado(ApplicationDbContext db) : IEquipoEliminacion
{
    public async Task AlEliminarEquipo(int idEquipo)
    {
        var details = await db
            .DetallesMantenimientos.Where(detail => detail.IdEquipo == idEquipo)
            .ToListAsync();

        foreach (var detail in details)
            detail.EstadoEliminado = true;
    }
}
