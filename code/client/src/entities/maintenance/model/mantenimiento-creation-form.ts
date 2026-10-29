export interface MantenimientoCreationForm {
  FechaMantenimiento: Date | null;
  FechaFinalDeMantenimiento: Date | null;
  NombreEmpresaMantenimiento: string | null;
  Costo: number | null;
  DescripcionMantenimiento: string | null;
}
