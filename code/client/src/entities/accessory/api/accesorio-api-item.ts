export interface AccesorioApiItem {
  Id: number;
  Nombre: string | null;
  Modelo: string | null;
  Tipo: string | null;
  Descripcion?: string | null;
  CodigoImtEquipoAsociado: string | null;
  Precio?: number | null;
  UrlDataSheet?: string | null;
  IdEquipo?: number | null;
  NombreEquipoAsociado?: string | null;
}
