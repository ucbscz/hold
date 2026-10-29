import { BaseModel } from '@shared/model';
export class Accesorio extends BaseModel {
  nombre: string | null = null;
  modelo: string | null = null;
  tipo: string | null = null;
  descripcion?: string | null = null;
  codigo_imt: string | null = null;
  precio?: number | null = null;
  url_data_sheet?: string | null = null;
  id_equipo?: number | null = null;
  nombreEquipoAsociado?: string | null = null;
}
