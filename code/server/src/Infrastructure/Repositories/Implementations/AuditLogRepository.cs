using System.Diagnostics.CodeAnalysis;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Core.Entities;
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

        if (entity == nameof(Usuario))
            return await _db.Usuarios
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => entityIds.Contains(item.Carnet))
                .ToDictionaryAsync(
                    item => item.Carnet,
                    item => (item.Nombre + " " + item.ApellidoPaterno + " " + item.ApellidoMaterno).Trim()
                );

        var ids = entityIds
            .Select(value => int.TryParse(value, out var id) ? id : (int?)null)
            .OfType<int>()
            .ToHashSet();
        if (ids.Count == 0)
            return [];

        var labels = entity switch
        {
            nameof(Ambiente) => await _db.Ambientes.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Procedencia) => await _db.Procedencias.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Carrera) => await _db.Carreras.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Categoria) => await _db.Categorias.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(GrupoEquipo) => await _db.GruposEquipos.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Mueble) => await _db.Muebles.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Gavetero) => await _db.Gaveteros.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Accesorio) => await _db.Accesorios.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Componente) => await _db.Componentes.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(EmpresaMantenimiento) => await _db.EmpresasMantenimiento.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(item.Id, item.Nombre))
                .ToListAsync(),
            nameof(Equipo) => await _db.Equipos.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(
                    item.Id,
                    "IMT " + item.CodigoImt + " · " + (item.GrupoEquipo != null ? item.GrupoEquipo.Nombre : "Equipo")
                ))
                .ToListAsync(),
            nameof(Prestamo) => await _db.Prestamos.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => ids.Contains(item.Id))
                .Select(item => new EntityLabel(
                    item.Id,
                    item.Usuario != null
                        ? "Préstamo de " + item.Usuario.Nombre + " " + item.Usuario.ApellidoPaterno
                        : "Préstamo"
                ))
                .ToListAsync(),
            nameof(Mantenimiento) => await (
                from maintenance in _db.Mantenimientos.AsNoTracking()
                    .IgnoreQueryFilters()
                join company in _db.EmpresasMantenimiento.AsNoTracking().IgnoreQueryFilters()
                    on maintenance.IdEmpresa equals company.Id
                where ids.Contains(maintenance.Id)
                select new EntityLabel(maintenance.Id, "Mantenimiento con " + company.Nombre)
            ).ToListAsync(),
            nameof(ConfiguracionSistema) => ids
                .Select(id => new EntityLabel(id, "Configuración del sistema"))
                .ToList(),
            _ => [],
        };

        return labels.ToDictionary(
            item => item.Id.ToString(CultureInfo.InvariantCulture),
            item => item.Name
        );
    }

    private sealed record EntityLabel(int Id, string Name);

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
