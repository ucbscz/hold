using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Inventario.Infrastructure;

public sealed class InventarioAuditNames(ApplicationDbContext db, EquipmentLabels labels)
    : IAuditEntityNames
{
    public async Task<Dictionary<string, string>?> FindNames(
        string entity,
        IReadOnlyCollection<string> ids
    )
    {
        var numericIds = AuditEntityLabel.NumericIds(ids);
        var found = entity switch
        {
            nameof(Ambiente) => await AuditEntityLabel.ByName(db.Ambientes, numericIds),
            nameof(Procedencia) => await AuditEntityLabel.ByName(db.Procedencias, numericIds),
            nameof(Categoria) => await AuditEntityLabel.ByName(db.Categorias, numericIds),
            nameof(GrupoEquipo) => await AuditEntityLabel.ByName(db.GruposEquipos, numericIds),
            nameof(Mueble) => await AuditEntityLabel.ByName(db.Muebles, numericIds),
            nameof(Gavetero) => await AuditEntityLabel.ByName(db.Gaveteros, numericIds),
            nameof(Accesorio) => await AuditEntityLabel.ByName(db.Accesorios, numericIds),
            nameof(Componente) => await AuditEntityLabel.ByName(db.Componentes, numericIds),
            nameof(Equipo) => await EquipmentNames(numericIds),
            _ => null,
        };

        return found == null ? null : AuditEntityLabel.ToNames(found);
    }

    private Task<List<AuditEntityLabel>> EquipmentNames(HashSet<int> ids)
    {
        var codePrefix = labels.InternalCodePrefix;
        return db.Equipos.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item => ids.Contains(item.Id))
            .Select(item => new AuditEntityLabel(
                item.Id,
                codePrefix + item.CodigoImt + " · " + (item.GrupoEquipo != null ? item.GrupoEquipo.Nombre : "Equipo")
            ))
            .ToListAsync();
    }
}
