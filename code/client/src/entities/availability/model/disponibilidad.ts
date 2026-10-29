export class Disponibilidad {
  Fecha: Date | null;
  IdGrupoEquipo: number;
  CantidadDisponible: number;
  TotalOperativo: number;
  constructor() {
    this.Fecha = null;
    this.IdGrupoEquipo = 0;
    this.CantidadDisponible = 0;
    this.TotalOperativo = 0;
  }
}
