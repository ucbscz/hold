export interface PrestamoInlineItem {
  Id: number;
  NombreGrupoEquipo: string | null;
  CodigoImt: string | null;
  CodigoUcb?: string | null;
  NumeroSerial?: string | null;
  UbicacionEquipo?: string | null;
  NombreMueble?: string | null;
  NombreGavetero?: string | null;
  UbicacionMueble?: string | null;
  EstadoPrestamo: string | null;
  FechaSolicitud: string | Date | null;
  FechaDevolucionEsperada: string | Date | null;
}
