using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Prestamos.Infrastructure;

public sealed class PrestamosDeUsuarioEliminado(ApplicationDbContext db) : IUsuarioEliminacion
{
    public async Task AlEliminarUsuario(string carnet)
    {
        var loans = await db.Prestamos.Where(loan => loan.Carnet == carnet).ToListAsync();
        var loanIds = loans.Select(loan => loan.Id).ToArray();
        var details = await db
            .DetallesPrestamos.Where(detail => loanIds.Contains(detail.IdPrestamo))
            .ToListAsync();

        foreach (var loan in loans)
        {
            loan.EstadoPrestamo = EstadoPrestamo.Cancelado;
            loan.EstadoEliminado = true;
        }

        foreach (var detail in details)
            detail.EstadoEliminado = true;
    }
}
