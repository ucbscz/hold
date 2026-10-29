import { AuditEquipmentDetail } from './audit-equipment-detail';

export interface AuditFieldChange {
  campo: string;
  anterior?: string | null;
  nuevo?: string | null;
  protegido?: boolean;
}

export interface AuditDataItem {
  etiqueta: string;
  valor: string;
}

export interface AuditObservationDetail {
  observacion?: string;
  equipos?: AuditEquipmentDetail[];
  texto?: string;
  usuarioNombre?: string;
  usuarioCarnet?: string;
  equiposPrestamo?: string;
  fechaInicio?: string;
  fechaDevolucion?: string;
  cambios?: AuditFieldChange[];
  datos?: AuditDataItem[];
}
