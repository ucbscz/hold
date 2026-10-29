namespace UCB_Hold.Server.Modules.Auditoria.Application;

public record AuditEntry(AuditAccion Accion, string Entidad, string? EntidadId, string? Detalle);
