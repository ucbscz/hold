namespace IMT_Reservas.Server.Application.Features.AuditLog;

public record AuditEntry(AuditAccion Accion, string Entidad, string? EntidadId, string? Detalle);
