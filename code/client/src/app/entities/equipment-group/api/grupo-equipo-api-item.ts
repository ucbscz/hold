export interface GrupoEquipoApiItem {
  Id: number;
  Nombre: string | null;
  Descripcion?: string | null;
  Modelo?: string | null;
  UrlDataSheet?: string | null;
  Marca?: string | null;
  UrlImagen?: string | null;
  NombreCategoria?: string | null;
  Cantidad?: number | null;
  CostoPromedio?: number | null;
  TiempoMaximoPrestamoDias?: number | null;
}
