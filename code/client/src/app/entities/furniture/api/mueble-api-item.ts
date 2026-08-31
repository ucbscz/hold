export interface MuebleApiItem {
  Id: number;
  Nombre: string | null;
  NumeroGaveteros: number | null;
  Ubicacion: string | null;
  IdAmbiente?: number | null;
  NombreAmbiente?: string | null;
  Tipo: string | null;
  Costo: number | null;
  Longitud: number | null;
  Profundidad: number | null;
  Altura: number | null;
}
