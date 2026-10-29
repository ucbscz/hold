using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Prestamos.Infrastructure;

public class AvisoDisponibilidadRepository
{
    private readonly ApplicationDbContext _db;

    public AvisoDisponibilidadRepository(ApplicationDbContext db) => _db = db;

    public async Task Add(string carnet, AvisoDisponibilidadDto dto)
    {
        _db.AvisosDisponibilidad.Add(
            new AvisoDisponibilidad
            {
                CarnetUsuario = carnet,
                IdGrupoEquipo = dto.IdGrupoEquipo!.Value,
                Fecha = dto.Fecha!.Value,
                Cantidad = dto.Cantidad is > 0 ? dto.Cantidad.Value : 1,
                Notificado = false,
                FechaCreacion = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    public async Task<List<AvisoDisponibilidad>> GetPending()
    {
        await using var _ = await _db.RunAsSystemAsync();
        return await _db
            .AvisosDisponibilidad.AsNoTracking()
            .Where(a => !a.Notificado && !a.EstadoEliminado)
            .ToListAsync();
    }

    public async Task MarkAsNotified(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
            return;

        await using var _ = await _db.RunAsSystemAsync();
        await _db
            .AvisosDisponibilidad.Where(a => ids.Contains(a.Id) && !a.EstadoEliminado)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Notificado, true));
    }
}
