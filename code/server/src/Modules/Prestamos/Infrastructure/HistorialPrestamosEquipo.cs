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
                (x, u) => new HistorialRow
                {
                    IdDetalle = x.d.Id,
                    Prestamo = x.p,
                    NombreUsuario = u.Nombre + " " + u.ApellidoPaterno,
                    EstadoEquipoRetorno = x.d.EstadoEquipoRetorno,
                }
            )
            .OrderByDescending(r => r.Prestamo.FechaPrestamo)
            .ToListAsync();

        return rows.GroupBy(r => r.Prestamo.Id)
            .Select(group => ToDto(group.OrderByDescending(r => r.IdDetalle).First()))
            .ToList();
    }

    private static HistorialEquipoDto ToDto(HistorialRow row) =>
        new()
        {
            IdPrestamo = row.Prestamo.Id,
            Carnet = row.Prestamo.Carnet,
            NombreUsuario = row.NombreUsuario,
            FechaPrestamo = row.Prestamo.FechaPrestamo,
            FechaDevolucionEsperada = row.Prestamo.FechaDevolucionEsperada,
            FechaDevolucion = row.Prestamo.FechaDevolucion,
            EstadoPrestamo = row.Prestamo.EstadoPrestamo.ToString().ToLowerInvariant(),
            EstadoEquipo = row.EstadoEquipoRetorno.HasValue
                ? EstadoEquipoNombre.ToName(row.EstadoEquipoRetorno.Value)
                : null,
            Observacion = row.Prestamo.Observacion,
        };

    private sealed class HistorialRow
    {
        public int IdDetalle { get; init; }
        public required Domain.Prestamo Prestamo { get; init; }
        public string? NombreUsuario { get; init; }
        public EstadoEquipo? EstadoEquipoRetorno { get; init; }
    }
}
