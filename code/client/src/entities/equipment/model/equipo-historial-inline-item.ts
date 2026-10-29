export interface EquipoHistorialInlineItem {
  IdPrestamo: number;
  NombreUsuario: string | null;
  Carnet: string | null;
  FechaPrestamo: string | Date | null;
  FechaDevolucionEsperada: string | Date | null;
  FechaDevolucion: string | Date | null;
  EstadoPrestamo: string | null;
  EstadoEquipo: string | null;
  Observacion: string | null;
}
