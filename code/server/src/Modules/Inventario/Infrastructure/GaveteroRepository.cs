using IMT_Reservas.Server.Application.Features.Gavetero;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using GaveteroEntity = IMT_Reservas.Server.Core.Entities.Gavetero;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class GaveteroRepository : Repository<GaveteroEntity, GaveteroDto>
{
    public GaveteroRepository(ApplicationDbContext dbContext, GaveteroMapper mapper)
        : base(dbContext, mapper) { }

    public async Task<int?> GetMuebleByNombre(string nombreMueble) =>
        await DbContext
            .Muebles.Where(m => m.Nombre == nombreMueble && !m.EstadoEliminado)
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

    public async Task<int?> GetMuebleByGavetero(int gaveteroId) =>
        await DbContext
            .Gaveteros.Where(g => g.Id == gaveteroId && !g.EstadoEliminado)
            .Select(g => g.IdMueble)
            .FirstOrDefaultAsync();

    public async Task RecalcMuebleCount(int muebleId)
    {
        var furniture = await DbContext.Muebles.FirstOrDefaultAsync(mueble => mueble.Id == muebleId);

        if (furniture == null)
            return;

        furniture.NumeroGaveteros = await DbContext.Gaveteros.CountAsync(gavetero =>
            gavetero.IdMueble == muebleId && !gavetero.EstadoEliminado
        );

        await DbContext.SaveChangesAsync();
    }

    public async Task<List<GaveteroDto>> GetByMueble(int muebleId) =>
        await ProjectTo(
                DbContext.Gaveteros.Where(g => g.IdMueble == muebleId && !g.EstadoEliminado)
            )
            .ToListAsync();
}
