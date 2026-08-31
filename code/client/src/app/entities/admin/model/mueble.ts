import { BaseModel } from '@shared/model';
export class Muebles extends BaseModel {
  Nombre: string | null = null;
  NumeroGaveteros: number | null = null;
  Ubicacion: string | null = null;
  IdAmbiente: number | null = null;
  NombreAmbiente: string | null = null;
  Tipo: string | null = null;
  Costo: number | null = null;
  Longitud: number | null = null;
  Profundidad: number | null = null;
  Altura: number | null = null;
  constructor() {
    super();
    this.Nombre = null;
    this.NumeroGaveteros = null;
    this.Ubicacion = null;
    this.IdAmbiente = null;
    this.NombreAmbiente = null;
    this.Tipo = null;
    this.Costo = null;
    this.Longitud = null;
    this.Profundidad = null;
    this.Altura = null;
  }
}
