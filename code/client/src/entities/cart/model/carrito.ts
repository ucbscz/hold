export interface Carrito {
  [id: number]: {
    nombre: string;
    modelo: string;
    marca: string;
    cantidad: number;
    fecha_inicio: string | null;
    fecha_final: string | null;
    imagen: string;
    precio: number;
    numero_serie_unico?: string[];
    codigo_ucb_unico?: string[];
    cantidadMax: number;
    tiempoMaximoPrestamoDias: number;
  };
}
