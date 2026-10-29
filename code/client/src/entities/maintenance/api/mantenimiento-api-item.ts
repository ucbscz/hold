export interface MantenimientoApiItem {
  Id: number;
  NombreEmpresaMantenimiento: string | null;
  FechaMantenimiento: Date | string | null;
  FechaFinalMantenimiento: Date | string | null;
  Costo: number | null;
  Descripcion: string | null;
  TipoMantenimiento: string | null;
  NombreGrupoEquipo: string | null;
  CodigoImtEquipo: string | null;
  DescripcionEquipo: string | null;
}
