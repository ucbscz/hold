import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AccesoriosService } from '@entities/accessory';
import { Accesorio } from '@entities/admin';
import { BuscadorComponent } from '@features/admin-search';
import { Tabla } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoEliminarComponent,
  AvisoExitoComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { AccesoriosCrearComponent } from '../accesorios-crear/accesorios-crear.component';
import { AccesoriosEditarComponent } from '../accesorios-editar/accesorios-editar.component';
@Component({
  selector: 'app-accesorios-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    AccesoriosCrearComponent,
    AccesoriosEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    Aviso,
    AvisoExitoComponent,
    BuscadorComponent,
    AuditPanelComponent,
  ],
  templateUrl: './accesorios-tabla.component.html',
  styleUrls: ['./accesorios-tabla.component.css'],
})
export class AccesoriosTablaComponent extends Tabla {
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }

  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  accesorios: Accesorio[] = [];
  accesorioscopia: Accesorio[] = [];
  accesorioSeleccionado: Accesorio = new Accesorio();
  override columnas: string[] = [
    'Nombre',
    'Modelo',
    'Tipo',
    'Código IMT del Equipo',
    'Precio',
  ];

  constructor(private readonly accesoriosapi: AccesoriosService) {
    super();
  }

  ngOnInit() {
    this.cargarAccesorios();
  }

  limpiarAccesorioSeleccionado() {
    this.accesorioSeleccionado = new Accesorio();
  }

  crearaccesorio() {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }

  cargarAccesorios() {
    this.accesoriosapi.obtenerAccesorios().subscribe({
      next: (data: Accesorio[]) => {
        this.accesorios = data;
        this.accesorioscopia = [...this.accesorios];
        this.aplicarFiltros();
        this.aplicarOrdenActualSiExiste();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los accesorios. Por favor, intente más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }

  aplicarFiltros(event?: [string, string]) {
    const busqueda = this.mantenerBusqueda(event);
    if (busqueda[0].trim() !== '') {
      const busquedaNormalizada = this.normalizeText(busqueda[0]);
      this.accesorios = this.accesorioscopia.filter((accesorio) => {
        switch (busqueda[1]) {
          case 'Nombre':
            return this.normalizeText(accesorio.nombre || '').includes(
              busquedaNormalizada,
            );
          case 'Modelo':
            return this.normalizeText(accesorio.modelo || '').includes(
              busquedaNormalizada,
            );
          case 'Tipo':
            return this.normalizeText(accesorio.tipo || '').includes(
              busquedaNormalizada,
            );
          case 'Código IMT del Equipo':
            return this.normalizeText(
              String(accesorio.codigo_imt || ''),
            ).includes(busquedaNormalizada);
          case 'Precio':
            return this.normalizeText(String(accesorio.precio || '')).includes(
              busquedaNormalizada,
            );
          default:
            return (
              this.normalizeText(accesorio.nombre || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(accesorio.modelo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(accesorio.tipo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(String(accesorio.codigo_imt || '')).includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(accesorio.nombreEquipoAsociado || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(String(accesorio.precio || '')).includes(
                busquedaNormalizada,
              )
            );
        }
      });
    } else {
      this.accesorios = [...this.accesorioscopia];
    }
    this.aplicarOrdenActualSiExiste();
  }

  limpiarBusqueda() {
    this.limpiarBusquedaPersistida();
    this.accesorios = [...this.accesorioscopia];
    this.aplicarOrdenActualSiExiste();
  }

  editarAccesorio(accesorio: Accesorio) {
    this.botoncrear.set(false);
    this.accesorioSeleccionado = { ...accesorio };
    this.botoneditar.set(true);
  }

  eliminarAccesorio(accesorio: Accesorio) {
    this.accesorioSeleccionado = accesorio;
    this.alertaeliminar = true;
  }

  confirmarEliminacion() {
    this.accesoriosapi
      .eliminarAccesorio(this.accesorioSeleccionado.Id)
      .subscribe({
        next: (_response) => {
          this.cargarAccesorios();
          this.mensajeexito = 'Accesorio eliminado exitosamente.';
          this.exito.set(true);
          this.auditRefresh++;
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al eliminar el accesorio. Por favor, intente más tarde.',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
    this.limpiarAccesorioSeleccionado();
    this.alertaeliminar = false;
  }

  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.limpiarAccesorioSeleccionado();
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.accesorios = this.sortByColumn(this.accesorios, e, {
      Nombre: (accesorio) => accesorio.nombre,
      Modelo: (accesorio) => accesorio.modelo,
      Tipo: (accesorio) => accesorio.tipo,
      'Código IMT del Equipo': (accesorio) => accesorio.codigo_imt,
      Precio: (accesorio) => accesorio.precio,
    });
  }
}
