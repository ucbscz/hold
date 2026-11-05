using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Usuarios.Infrastructure;

public sealed class UsuariosAuditNames(ApplicationDbContext db) : IAuditEntityNames
{
    public async Task<Dictionary<string, string>?> FindNames(
        string entity,
        IReadOnlyCollection<string> ids
    )
    {
        if (entity == nameof(Usuario))
            return await db.Usuarios.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Carnet))
                .ToDictionaryAsync(
                    item => item.Carnet,
                    item => (item.Nombre + " " + item.ApellidoPaterno + " " + item.ApellidoMaterno).Trim()
                );

        if (entity != nameof(Carrera))
            return null;

        return AuditEntityLabel.ToNames(
            await AuditEntityLabel.ByName(db.Carreras, AuditEntityLabel.NumericIds(ids))
        );
    }
}
