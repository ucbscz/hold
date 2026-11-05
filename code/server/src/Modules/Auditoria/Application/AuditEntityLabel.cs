using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Auditoria.Application;

public sealed record AuditEntityLabel(int Id, string Name)
{
    private const string NameColumn = "Nombre";

    public static HashSet<int> NumericIds(IEnumerable<string> ids) =>
        ids.Select(value => int.TryParse(value, out var id) ? id : (int?)null)
            .OfType<int>()
            .ToHashSet();

    public static Task<List<AuditEntityLabel>> ByName<TEntity>(
        IQueryable<TEntity> source,
        IReadOnlyCollection<int> ids
    )
        where TEntity : Entity =>
        source
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item => ids.Contains(item.Id))
            .Select(item => new AuditEntityLabel(item.Id, EF.Property<string>(item, NameColumn)))
            .ToListAsync();

    public static Dictionary<string, string> ToNames(IEnumerable<AuditEntityLabel> labels) =>
        labels.ToDictionary(
            item => item.Id.ToString(CultureInfo.InvariantCulture),
            item => item.Name
        );
}
