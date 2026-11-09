import { INVENTORY_LABELS } from '@shared/config';
import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  Equipos,
  EquipoService,
  ESTADO_EQUIPO_OPCIONES,
  HistorialEquipoInlineComponent,
} from '@entities/equipment';
import {
  AvisoEliminarComponent,
  BuscadorComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
import {
  AdminTabsComponent,
  ColumnaOrdenableComponent,
  TablaGestion,
  TablePaginationComponent,
} from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import { AuditPanelComponent } from '@entities/audit-log';
import { EquiposFormularioComponent } from '../equipos-formulario/equipos-formulario.component';

@Component({
  selector: 'app-equipos-tabla',
  standalone: true,
  imports: [
    ColumnaOrdenableComponent,
    AdminTabsComponent,
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    EquiposFormularioComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    BuscadorComponent,
    HistorialEquipoInlineComponent,
    AuditPanelComponent,
    TablePaginationComponent,
    CustomSelectComponent,
  ],
  templateUrl: './equipos-tabla.component.html',
  styleUrls: [
    '../../../../shared/lib/admin-table/tabla-admin.css',
    './equipos-tabla.component.css',
  ],
})
export class EquiposTablaComponent extends TablaGestion {
  obsHistorial: string | null = null;
  abrirObsHistorial(obs: string) {
    this.obsHistorial = obs;
  }
  cerrarObsHistorial() {
    this.obsHistorial = null;
  }

  equipos: Equipos[] = [];
  equiposcopia: Equipos[] = [];
  equipoSeleccionado: Equipos = new Equipos();
  override columnas: string[] = [
    'Nombre',
    'EstadoEquipo',
    'Ubicacion',
    INVENTORY_LABELS.internalCode,
    'Costo',
  ];

  estadoSeleccionado = '';
  readonly estadoFiltroOpciones: OpcionSelect[] = [
    { value: '', label: 'Todos los estados' },
    ...ESTADO_EQUIPO_OPCIONES,
  ];
  private busquedaActual?: [string, string];
  seleccionarEstado(estado: string) {
    this.estadoSeleccionado = estado;
    this.aplicarFiltros();
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
  actualizarDatos(): void {
    this.botoncrear.set(false);
    this.botoneditar.set(false);
    this.cargarEquipos();
    this.auditRefresh++;
  }

  cargarEquipos() {
    this.equiposapi.obtenerEquipos().subscribe({
      next: (data: Equipos[]) => {
        this.equipos = data;
        this.equiposcopia = [...this.equipos];
        this.aplicarFiltros();
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
          case INVENTORY_LABELS.internalCode:
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
    this.busquedaActual = undefined;
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
        this.toast.success('Equipo eliminado con éxito.');
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
      [INVENTORY_LABELS.internalCode]: (equipo) => equipo.CodigoImt,
      Costo: (equipo) => equipo.CostoReferencia,
    });
  }
}
