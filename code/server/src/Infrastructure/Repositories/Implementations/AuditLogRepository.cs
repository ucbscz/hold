using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using AuditLogEntity = IMT_Reservas.Server.Core.Entities.AuditLog;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class AuditLogRepository
{
    private readonly ApplicationDbContext _db;

    public AuditLogRepository(ApplicationDbContext db) => _db = db;

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
            await _db.SaveChangesAsync();
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
        await _db.SaveChangesAsync();
    }

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
            var normalizedActor = actor.Trim().ToLower();
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

        return await query
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
