export interface ComponenteApiItem {
  Id: number;
  Nombre: string | null;
  Modelo: string | null;
  Tipo: string | null;
  Descripcion?: string | null;
  PrecioReferencia?: number | null;
  NombreEquipo?: string | null;
  CodigoImtEquipo: string | null;
  UrlDataSheet?: string | null;
  IdEquipo?: number | null;
}
