namespace IMT_Reservas.Server.Application.Features.AuditLog;

public class AuditLogDto
{
    public int? Id { get; set; }
    public string? AdminCarnet { get; set; }
    public string? AdminNombre { get; set; }
    public string? Accion { get; set; }
    public string? Entidad { get; set; }
    public string? EntidadId { get; set; }
    public string? EntidadNombre { get; set; }
    public string? Detalle { get; set; }
    public DateTime? Timestamp { get; set; }
}
