import { AuditEquipmentDetail } from './audit-equipment-detail';

export interface AuditObservationDetail {
  observacion?: string;
  equipos?: AuditEquipmentDetail[];
  texto?: string;
  usuarioNombre?: string;
  usuarioCarnet?: string;
  equiposPrestamo?: string;
  fechaInicio?: string;
  fechaDevolucion?: string;
}
