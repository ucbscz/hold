import { BaseModel } from '@shared/model';
export class Componente extends BaseModel {
  Nombre: string | null = null;
  Modelo: string | null = null;
  Tipo: string | null = null;
  Descripcion?: string | null = null;
  PrecioReferencia?: number | null = null;
  NombreEquipo?: string | null = null;
  CodigoImtEquipo: string | null = null;
  UrlDataSheet?: string | null = null;
  IdEquipo?: number | null = null;
}
