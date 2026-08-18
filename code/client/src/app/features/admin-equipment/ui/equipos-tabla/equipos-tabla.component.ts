import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Equipos } from '@entities/admin';
import { EquipoService } from '@entities/equipment';
import { BuscadorComponent } from '@features/admin-search';
import { Tabla, TablePaginationComponent } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoEliminarComponent,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
import { HistorialEquipoInlineComponent } from '@widgets/admin-inline';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { EquiposCrearComponent } from '../equipos-crear/equipos-crear.component';
import { EquiposEditarComponent } from '../equipos-editar/equipos-editar.component';

@Component({
  selector: 'app-equipos-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    EquiposCrearComponent,
    EquiposEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    HistorialEquipoInlineComponent,
    AuditPanelComponent,
    TablePaginationComponent,
    CustomSelectComponent,
  ],
  templateUrl: './equipos-tabla.component.html',
  styleUrls: ['./equipos-tabla.component.css'],
})
export class EquiposTablaComponent extends Tabla {
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }

  obsHistorial: string | null = null;
  abrirObsHistorial(obs: string) {
    this.obsHistorial = obs;
  }
  cerrarObsHistorial() {
    this.obsHistorial = null;
  }

  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  equipos: Equipos[] = [];
  equiposcopia: Equipos[] = [];
  equipoSeleccionado: Equipos = new Equipos();
  override columnas: string[] = [
    'Nombre',
    'EstadoEquipo',
    'Ubicacion',
    'Código IMT',
    'Costo',
  ];

  estadoSeleccionado = '';
  estadosDisponibles = ['operativo', 'parcialmente_operativo', 'inoperativo'];
  readonly estadoFiltroOpciones: OpcionSelect[] = [
    { value: '', label: 'Todos los estados' },
    { value: 'operativo', label: 'Operativo' },
    { value: 'parcialmente_operativo', label: 'Parcialmente operativo' },
    { value: 'inoperativo', label: 'Inoperativo' },
  ];
  private busquedaActual?: [string, string];
  seleccionarEstado(estado: string) {
    this.estadoSeleccionado = estado;
    this.aplicarFiltros();
  }
  estadoEquipoLabel(estado: string): string {
    switch (estado) {
      case 'operativo':
        return 'Operativo';
      case 'parcialmente_operativo':
        return 'Parcialmente operativo';
      case 'inoperativo':
        return 'Inoperativo';
      default:
        return estado;
    }
  }
  constructor(private readonly equiposapi: EquipoService) {
    super();
  }
  ngOnInit() {
    this.cargarEquipos();
  }
  limpiarEquipoSeleccionado() {
    this.equipoSeleccionado = new Equipos();
  }
  crearequipo() {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }
  cargarEquipos() {
    this.equiposapi.obtenerEquipos().subscribe({
      next: (data: Equipos[]) => {
        this.equipos = data;
        this.equiposcopia = [...this.equipos];
        this.aplicarOrdenActualSiExiste();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los equipos',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  aplicarFiltros(event?: [string, string]) {
    if (event) this.busquedaActual = event;
    let lista = [...this.equiposcopia];
    const ev = this.busquedaActual;
    if (ev && ev[0].trim() !== '') {
      const busquedaNormalizada = this.normalizeText(ev[0]);
      lista = lista.filter((equipo) => {
        switch (ev[1]) {
          case 'Nombre':
            return this.normalizeText(equipo.NombreGrupoEquipo || '').includes(
              busquedaNormalizada,
            );
          case 'EstadoEquipo':
            return this.normalizeText(equipo.EstadoEquipo || '').includes(
              busquedaNormalizada,
            );
          case 'Ubicacion':
            return this.normalizeText(equipo.Ubicacion || '').includes(
              busquedaNormalizada,
            );
          case 'Código IMT':
            return this.normalizeText(String(equipo.CodigoImt || '')).includes(
              busquedaNormalizada,
            );
          case 'Costo':
            return this.normalizeText(
              String(equipo.CostoReferencia || ''),
            ).includes(busquedaNormalizada);
          default:
            return (
              this.normalizeText(equipo.NombreGrupoEquipo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(equipo.EstadoEquipo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(equipo.Ubicacion || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(String(equipo.CodigoImt || '')).includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(String(equipo.CostoReferencia || '')).includes(
                busquedaNormalizada,
              )
            );
        }
      });
    }
    if (this.estadoSeleccionado !== '') {
      lista = lista.filter(
        (equipo) => (equipo.EstadoEquipo || '') === this.estadoSeleccionado,
      );
    }
    this.equipos = lista;
    this.reiniciarPaginacion();
    this.aplicarOrdenActualSiExiste();
  }
  limpiarBusqueda() {
    this.equipos = [...this.equiposcopia];
    this.aplicarOrdenActualSiExiste();
  }
  editarEquipo(equipo: Equipos) {
    this.botoncrear.set(false);
    this.equipoSeleccionado = { ...equipo };
    this.botoneditar.set(true);
  }
  eliminarEquipo(equipo: Equipos) {
    this.equipoSeleccionado = equipo;
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    this.equiposapi.eliminarEquipo(this.equipoSeleccionado.Id).subscribe({
      next: (_response) => {
        this.mensajeexito = 'Equipo eliminado con éxito';
        this.exito.set(true);
        this.auditRefresh++;
        this.cargarEquipos();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al eliminar el equipo',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
    this.limpiarEquipoSeleccionado();
    this.alertaeliminar = false;
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.limpiarEquipoSeleccionado();
  }

  detenerPropagacion(event: Event): void {
    event.stopPropagation();
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.equipos = this.sortByColumn(this.equipos, e, {
      Nombre: (equipo) => equipo.NombreGrupoEquipo,
      EstadoEquipo: (equipo) => equipo.EstadoEquipo,
      Ubicacion: (equipo) => equipo.Ubicacion,
      'Código IMT': (equipo) => equipo.CodigoImt,
      Costo: (equipo) => equipo.CostoReferencia,
    });
  }
}
