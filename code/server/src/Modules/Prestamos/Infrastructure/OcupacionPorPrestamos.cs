using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Mantenimientos.Application;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Prestamos.Infrastructure;

public sealed class OcupacionPorPrestamos(ApplicationDbContext db) : IOcupacionPorPrestamos
{
    public Task<bool> HayPrestamoEnRango(
        IReadOnlyCollection<int> codigosImt,
        DateTime start,
        DateTime end
    )
    {
        var equipmentIds = db
            .Equipos.Where(equipment =>
                codigosImt.Contains(equipment.CodigoImt) && !equipment.EstadoEliminado
            )
            .Select(equipment => equipment.Id);

        return db
            .DetallesPrestamos.Join(
                db.Prestamos,
                detail => detail.IdPrestamo,
                loan => loan.Id,
                (detail, loan) => new { Detail = detail, Loan = loan }
            )
            .AnyAsync(item =>
                item.Detail.IdEquipo != null
                && equipmentIds.Contains(item.Detail.IdEquipo.Value)
                && PrestamoAvailabilityPolicy.BlockingStates.Contains(item.Loan.EstadoPrestamo)
                && item.Loan.FechaPrestamoEsperada < end
                && item.Loan.FechaDevolucionEsperada > start
            );
    }
}
