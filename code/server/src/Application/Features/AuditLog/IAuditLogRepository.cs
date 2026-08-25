namespace IMT_Reservas.Server.Application.Features.AuditLog;

public interface IAuditLogRepository
{
    Task WriteLog(
        AuditAccion accion,
        string entidad,
        string? entidadId,
        string? detalle,
        string adminCarnet,
        string adminNombre,
        bool saveChanges = true
    );

    Task WriteMany(
        IReadOnlyCollection<AuditEntry> entries,
        string adminCarnet,
        string adminNombre
    );

    Task<List<AuditLogDto>> GetFiltered(
        string? entidad,
        string? actor,
        string? accion,
        DateTime? desde,
        DateTime? hasta
    );
}
