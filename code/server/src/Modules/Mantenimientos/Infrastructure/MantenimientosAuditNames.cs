using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Mantenimientos.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Mantenimientos.Infrastructure;

public sealed class MantenimientosAuditNames(ApplicationDbContext db) : IAuditEntityNames
{
    public async Task<Dictionary<string, string>?> FindNames(
        string entity,
        IReadOnlyCollection<string> ids
    )
    {
        var numericIds = AuditEntityLabel.NumericIds(ids);
        var found = entity switch
        {
            nameof(EmpresaMantenimiento) => await AuditEntityLabel.ByName(
                db.EmpresasMantenimiento,
                numericIds
            ),
            nameof(Mantenimiento) => await (
                from maintenance in db.Mantenimientos.AsNoTracking().IgnoreQueryFilters()
                join company in db.EmpresasMantenimiento.AsNoTracking().IgnoreQueryFilters()
                    on maintenance.IdEmpresa equals company.Id
                where numericIds.Contains(maintenance.Id)
                select new AuditEntityLabel(maintenance.Id, "Mantenimiento con " + company.Nombre)
            ).ToListAsync(),
            _ => null,
        };

        return found == null ? null : AuditEntityLabel.ToNames(found);
    }
}
