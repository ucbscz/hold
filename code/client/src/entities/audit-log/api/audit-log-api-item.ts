export interface AuditLogApiItem {
  Id: number;
  AdminCarnet: string | null;
  AdminNombre: string | null;
  Accion: string | null;
  Entidad: string | null;
  EntidadId: string | number | null;
  EntidadNombre?: string | null;
  Detalle: string | null;
  Timestamp?: string | Date | null;
}
