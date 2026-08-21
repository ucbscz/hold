export interface EquipoApiItem {
  Id: number;
  NombreGrupoEquipo: string | null;
  IdGrupoEquipo: number | null;
  CodigoImt: number | null;
  CodigoUcb: string | null;
  NumeroSerial: string | null;
  EstadoEquipo: string | null;
  Ubicacion: string | null;
  NombreGavetero: string | null;
  IdGavetero: number | null;
  CostoReferencia: number | null;
  Descripcion: string | null;
  Procedencia: string | null;
  FechaIngresoEquipo: Date | null;
}
