export interface PrestamoApiItem {
  Id: number;
  CarnetUsuario: string | null;
  NombreUsuario: string | null;
  ApellidoPaternoUsuario: string | null;
  TelefonoUsuario: string | null;
  NombreGrupoEquipo: string | null;
  CodigoImt: string | null;
  FechaSolicitud: string | Date | null;
  FechaPrestamoEsperada: string | Date | null;
  FechaPrestamo: string | Date | null;
  FechaDevolucionEsperada: string | Date | null;
  FechaDevolucion: string | Date | null;
  Observacion: string | null;
  EstadoPrestamo: string | null;
  IdContrato: string | null;
  DestinoPrestamo: string;
  UbicacionEquipo?: string | null;
  Ubicacion_Equipo?: string | null;
  NombreGavetero?: string | null;
  Nombre_Gavetero?: string | null;
  NombreMueble?: string | null;
  Nombre_Mueble?: string | null;
  UbicacionMueble?: string | null;
  Ubicacion_Mueble?: string | null;
  TipoUsuario?: string | null;
  IdCarrera?: number | null;
  NombreMateria?: string | null;
}
