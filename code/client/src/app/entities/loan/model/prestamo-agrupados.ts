import { PrestamoDto } from '@entities/admin';

export class PrestamoAgrupados {
  datosgrupo: PrestamoDto;
  equipos: PrestamoDto[];

  constructor(equipos: PrestamoDto[]) {
    this.equipos = equipos;
    this.datosgrupo = { ...equipos[0] };
  }

  insertarEquipo(equipo: PrestamoDto): void {
    this.equipos.push(equipo);
  }
}
