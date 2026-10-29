export interface HistorialEquipoDto {
  IdPrestamo?: number;
  Carnet?: string;
  NombreUsuario?: string;
  FechaPrestamo?: Date;
  FechaDevolucionEsperada?: Date;
  FechaDevolucion?: Date;
  EstadoPrestamo?: string;
  EstadoEquipo?: string;
  Observacion?: string;
}
