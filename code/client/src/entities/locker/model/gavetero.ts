import { BaseModel } from '@shared/model';
export class Gaveteros extends BaseModel {
  Nombre: string | null = null;
  Tipo: string | null = null;
  NombreMueble: string | null = null;
  Longitud: number | null = null;
  Profundidad: number | null = null;
  Altura: number | null = null;
  constructor() {
    super();
    this.Nombre = null;
    this.Tipo = null;
    this.NombreMueble = null;
    this.Longitud = null;
    this.Profundidad = null;
    this.Altura = null;
  }
}
