import { CommonModule } from '@angular/common';
import { Component, Input, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Equipos } from '@entities/admin';
import { EquipoService } from '@entities/equipment';
import { AdminTableSort, Tabla } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { MostrarerrorComponent } from '@shared/ui';
import { MantenimientosServiceEquipos } from '../../../model/mantenimientos-equipos.service';
import { FormularioDatosComponent } from './formulario-datos/formulario-datos.component';

@Component({
  selector: 'app-lista-equipo',
  imports: [
    CommonModule,
    FormsModule,
    FormularioDatosComponent,
    MostrarerrorComponent,
  ],
  templateUrl: './lista-equipo.component.html',
  styleUrl: './lista-equipo.component.css',
})
export class ListaEquipoComponent extends Tabla {
  @Input() agregarequipo: WritableSignal<boolean> = signal(true);
  equipos: Equipos[] = [];
  equiposcopia: Equipos[] = [];
  equipoSeleccionado: Equipos = new Equipos();
  terminoBusqueda: string = '';
  agregarEquipoSeleccionado: WritableSignal<boolean> = signal(false);
  override sortColumn = 'Nombre';
  override columnas = ['Nombre', 'Estado', 'Ubicación', 'Código IMT', 'Costo'];

  get equiposDisponibles(): Equipos[] {
    return this.equipos.filter((equipo) => !this.equipoYaSeleccionado(equipo));
  }

  constructor(
    private readonly equiposapi: EquipoService,
    public mantenimientoequipos: MantenimientosServiceEquipos,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarEquipos();
  }
  agregarEquipo(equipo: Equipos) {
    this.equipoSeleccionado = equipo;
    this.agregarEquipoSeleccionado.set(true);
  }

  equipoYaSeleccionado(equipo: Equipos): boolean {
    return (
      equipo.CodigoImt !== null &&
      this.mantenimientoequipos.existeEquipo(equipo.CodigoImt)
    );
  }

  limpiarEquipoSeleccionado() {
    this.equipoSeleccionado = new Equipos();
  }
  cargarEquipos() {
    this.equiposapi.obtenerEquipos().subscribe({
      next: (data: Equipos[]) => {
        this.equipos = data;
        this.equiposcopia = [...this.equipos];
        this.buscar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los equipos, intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  override aplicarFiltros(event?: [string, string]): void {
    this.terminoBusqueda = event?.[0] ?? this.terminoBusqueda;
    this.buscar();
  }
  buscar() {
    if (this.terminoBusqueda.trim() === '') {
      this.limpiarBusqueda();
      return;
    }
    const busquedaNormalizada = this.normalizeText(this.terminoBusqueda);
    this.equipos = this.equiposcopia.filter(
      (equipo) =>
        this.normalizeText(equipo.NombreGrupoEquipo).includes(
          busquedaNormalizada,
        ) ||
        this.normalizeText(equipo.Modelo).includes(busquedaNormalizada) ||
        this.normalizeText(equipo.Marca).includes(busquedaNormalizada) ||
        this.normalizeText(String(equipo.CodigoImt || '')).includes(
          busquedaNormalizada,
        ) ||
        this.normalizeText(equipo.CodigoUcb).includes(busquedaNormalizada) ||
        this.normalizeText(equipo.NumeroSerial).includes(busquedaNormalizada),
    );
    this.aplicarOrdenActualSiExiste();
  }
  limpiarBusqueda() {
    this.terminoBusqueda = '';
    this.equipos = [...this.equiposcopia];
    this.aplicarOrdenActualSiExiste();
  }
  override sortTable(sort: AdminTableSort): void {
    this.equipos = this.sortByColumn(this.equipos, sort, {
      Nombre: (equipo) => equipo.NombreGrupoEquipo,
      Estado: (equipo) => equipo.EstadoEquipo,
      Ubicación: (equipo) => equipo.Ubicacion,
      'Código IMT': (equipo) => equipo.CodigoImt,
      Costo: (equipo) => equipo.CostoReferencia,
    });
  }
  regresar() {
    this.agregarequipo.set(false);
  }
}
