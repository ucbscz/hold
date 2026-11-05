using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using AuditLogEntity = UCB_Hold.Server.Modules.Auditoria.Domain.AuditLog;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Auditoria.Infrastructure;

public class AuditLogRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IReadOnlyList<IAuditEntityNames> _entityNames;

    public AuditLogRepository(ApplicationDbContext db, IEnumerable<IAuditEntityNames> entityNames)
    {
        _db = db;
        _entityNames = entityNames.ToList();
    }

    public async Task WriteLog(
        AuditAccion accion,
        string entidad,
        string? entidadId,
        string? detalle,
        string adminCarnet,
        string adminNombre,
        bool saveChanges = true
    )
    {
        _db.AuditLogs.Add(
            BuildLog(accion, entidad, entidadId, detalle, adminCarnet, adminNombre, DateTime.UtcNow)
        );
        if (saveChanges)
            await SaveAsSystem();
    }

    public async Task WriteMany(
        IReadOnlyCollection<AuditEntry> entries,
        string adminCarnet,
        string adminNombre
    )
    {
        if (entries.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var logs = entries
            .Select(entry =>
                BuildLog(
                    entry.Accion,
                    entry.Entidad,
                    entry.EntidadId,
                    entry.Detalle,
                    adminCarnet,
                    adminNombre,
                    now
                )
            )
            .ToList();

        _db.AuditLogs.AddRange(logs);
        await SaveAsSystem();
    }

    private async Task SaveAsSystem()
    {
        await using var _ = await _db.RunAsSystemAsync();
        await _db.SaveChangesAsync();
    }

    [SuppressMessage("Globalization", "CA1304", Justification = SqlTranslation.Justification)]
    [SuppressMessage("Globalization", "CA1311", Justification = SqlTranslation.Justification)]
    public async Task<List<AuditLogDto>> GetFiltered(
        string? entidad,
        string? actor,
        string? accion,
        DateTime? desde,
        DateTime? hasta,
        string? buscar = null
    )
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entidad))
            query = query.Where(a => a.Entidad == entidad);
        if (!string.IsNullOrWhiteSpace(actor))
        {
            var normalizedActor = actor.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.AdminCarnet.ToLower().Contains(normalizedActor)
                || a.AdminNombre.ToLower().Contains(normalizedActor)
            );
        }
        if (!string.IsNullOrWhiteSpace(accion))
            query = query.Where(a => a.Accion == accion);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var normalizedSearch = buscar.Trim().ToLowerInvariant();
            var hasSearchDate = DateTime.TryParseExact(
                buscar.Trim(),
                ["d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "yyyy-MM-dd"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var searchDate
            );
            var searchDateStart = hasSearchDate
                ? searchDate.Date.ToUniversalTime()
                : DateTime.MinValue;
            var searchDateEnd = hasSearchDate
                ? searchDate.Date.AddDays(1).ToUniversalTime()
                : DateTime.MinValue;

            query = query.Where(a =>
                a.AdminCarnet.ToLower().Contains(normalizedSearch)
                || a.AdminNombre.ToLower().Contains(normalizedSearch)
                || a.Accion.ToLower().Contains(normalizedSearch)
                || a.Entidad.ToLower().Contains(normalizedSearch)
                || (a.EntidadId != null && a.EntidadId.ToLower().Contains(normalizedSearch))
                || (a.Detalle != null && a.Detalle.ToLower().Contains(normalizedSearch))
                || (hasSearchDate && a.Timestamp >= searchDateStart && a.Timestamp < searchDateEnd)
            );
        }
        if (desde.HasValue)
            query = query.Where(a => a.Timestamp >= desde.Value.ToUniversalTime());
        if (hasta.HasValue)
            query = query.Where(a => a.Timestamp <= hasta.Value.ToUniversalTime());

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(300)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                AdminCarnet = a.AdminCarnet,
                AdminNombre = a.AdminNombre,
                Accion = a.Accion,
                Entidad = a.Entidad,
                EntidadId = a.EntidadId,
                Detalle = a.Detalle,
                Timestamp = a.Timestamp,
            })
            .ToListAsync();

        await PopulateEntityNames(logs);
        return logs;
    }

    private async Task PopulateEntityNames(List<AuditLogDto> logs)
    {
        foreach (var entityLogs in logs.GroupBy(log => log.Entidad ?? string.Empty))
        {
            var names = await GetEntityNames(
                entityLogs.Key,
                entityLogs.Select(log => log.EntidadId).OfType<string>().ToHashSet()
            );

            foreach (var log in entityLogs)
                log.EntidadNombre = log.EntidadId != null
                    ? names.GetValueOrDefault(log.EntidadId)
                    : null;
        }
    }

    private async Task<Dictionary<string, string>> GetEntityNames(
        string entity,
        HashSet<string> entityIds
    )
    {
        if (entityIds.Count == 0)
            return [];

        foreach (var provider in _entityNames)
        {
            var names = await provider.FindNames(entity, entityIds);
            if (names != null)
                return names;
        }

        return [];
    }

    private static AuditLogEntity BuildLog(
        AuditAccion accion,
        string entidad,
        string? entidadId,
        string? detalle,
        string adminCarnet,
        string adminNombre,
        DateTime timestamp
    ) =>
        new()
        {
            AdminCarnet = adminCarnet,
            AdminNombre = adminNombre,
            Accion = accion.ToString(),
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle,
            Timestamp = timestamp,
        };
}
