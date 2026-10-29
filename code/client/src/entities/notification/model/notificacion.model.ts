export class Notificacion {
  Id: number;
  CarnetUsuario: string | null;
  Tipo: string | null;
  Titulo: string | null;
  Contenido: string | null;
  Detalle: string | null;
  FechaEnvio: string | null;
  Leido: boolean;

  constructor() {
    this.Id = 0;
    this.CarnetUsuario = null;
    this.Tipo = null;
    this.Titulo = null;
    this.Contenido = null;
    this.Detalle = null;
    this.FechaEnvio = null;
    this.Leido = false;
  }
}
