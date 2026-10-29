export class GrupoEquipo {
  id: number = 0;
  nombre: string | null = null;
  modelo?: string | null = null;
  nombreCategoria?: string | null = null;
  IdCategoria?: number | null = null;
  descripcion?: string | null = null;
  url_data_sheet?: string | null = null;
  marca?: string | null = null;
  link?: string | null = null;
  Cantidad?: number | null = null;
  CostoPromedio?: number | null = null;
  TiempoMaximoPrestamoDias: number = 7;
}
