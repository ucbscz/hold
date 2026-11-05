using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Prestamos.Infrastructure;

public sealed class HistorialPrestamosEquipo(ApplicationDbContext db) : IHistorialPrestamosEquipo
{
    public async Task<List<HistorialEquipoDto>> GetHistorial(int equipoId)
    {
        var rows = await db
            .DetallesPrestamos.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => d.IdEquipo == equipoId)
            .Join(
                db.Prestamos.IgnoreQueryFilters(),
                d => d.IdPrestamo,
                p => p.Id,
                (d, p) => new { d, p }
            )
            .Join(
                db.Usuarios.IgnoreQueryFilters(),
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
                        ? EstadoEquipoNombre.ToName(r.EstadoEquipoRetorno.Value)
                        : null,
                Observacion = r.Observacion,
            })
            .ToList();
    }
}
