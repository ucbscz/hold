import { Injectable } from '@angular/core';
import { EquipoMantenimientoSeleccionado } from './equipo-mantenimiento-seleccionado';

@Injectable({
  providedIn: 'root',
})
export class MantenimientosServiceEquipos {
  private readonly equiposSeleccionados = new Map<
    number,
    EquipoMantenimientoSeleccionado
  >();

  obtenerEquiposMantenimientos(): Map<number, EquipoMantenimientoSeleccionado> {
    return this.equiposSeleccionados;
  }

  agregarEquipoMantenimiento(
    codigo: number,
    tipo: string,
    descripcion: string,
    nombre: string,
  ): void {
    this.equiposSeleccionados.set(codigo, {
      nombre,
      TipoMantenimiento: tipo,
      DescripcionEquipo: descripcion,
    });
  }

  existeEquipo(codigo: number): boolean {
    return this.equiposSeleccionados.has(codigo);
  }

  quitarEquipo(codigo: number): void {
    this.equiposSeleccionados.delete(codigo);
  }

  actualizarTipo(codigo: number, tipo: string): void {
    const equipo = this.equiposSeleccionados.get(codigo);
    if (!equipo) return;

    this.equiposSeleccionados.set(codigo, {
      ...equipo,
      TipoMantenimiento: tipo,
    });
  }

  vaciarEquiposMantenimientos(): void {
    this.equiposSeleccionados.clear();
  }
}
