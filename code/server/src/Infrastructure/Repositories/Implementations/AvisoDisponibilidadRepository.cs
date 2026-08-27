using IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

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

    public async Task<List<AvisoDisponibilidad>> GetPending() =>
        await _db
            .AvisosDisponibilidad.AsNoTracking()
            .Where(a => !a.Notificado && !a.EstadoEliminado)
            .ToListAsync();

    public async Task MarkAsNotified(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
            return;

        await _db
            .AvisosDisponibilidad.Where(a => ids.Contains(a.Id) && !a.EstadoEliminado)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Notificado, true));
    }
}
