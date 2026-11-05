using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Prestamos.Infrastructure;

public sealed class PrestamosAuditNames(ApplicationDbContext db) : IAuditEntityNames
{
    public async Task<Dictionary<string, string>?> FindNames(
        string entity,
        IReadOnlyCollection<string> ids
    )
    {
        if (entity != nameof(Prestamo))
            return null;

        var numericIds = AuditEntityLabel.NumericIds(ids);
        return AuditEntityLabel.ToNames(
            await db.Prestamos.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => numericIds.Contains(item.Id))
                .Select(item => new AuditEntityLabel(
                    item.Id,
                    item.Usuario != null
                        ? "Préstamo de " + item.Usuario.Nombre + " " + item.Usuario.ApellidoPaterno
                        : "Préstamo"
                ))
                .ToListAsync()
        );
    }
}
