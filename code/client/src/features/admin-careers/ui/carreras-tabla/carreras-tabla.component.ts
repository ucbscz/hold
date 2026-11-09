import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Carrera, CarreraService } from '@entities/career';
import {
  AvisoEliminarComponent,
  BuscadorComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { AdminTabsComponent, TablaGestion } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import { AuditPanelComponent } from '@entities/audit-log';
import { CarrerasFormularioComponent } from '../carreras-formulario/carreras-formulario.component';
@Component({
  selector: 'app-carreras-tabla',
  standalone: true,
  imports: [
    AdminTabsComponent,
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    CarrerasFormularioComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    BuscadorComponent,
    AuditPanelComponent,
  ],
  templateUrl: './carreras-tabla.component.html',
  styleUrls: [
    '../../../../shared/lib/admin-table/tabla-admin.css',
    './carreras-tabla.component.css',
  ],
})
export class CarrerasTablaComponent extends TablaGestion {
  carreras: Carrera[] = [];
  carrerascopia: Carrera[] = [];
  carreraSeleccionada: Carrera = new Carrera();
  override columnas: string[] = ['Nombre', 'Código'];

  constructor(private readonly carreraService: CarreraService) {
    super();
  }

  ngOnInit(): void {
    this.cargarCarreras();
  }

  limpiarCarreraSeleccionada(): void {
    this.carreraSeleccionada = new Carrera();
  }

  crearCarrera(): void {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }

  actualizarDatos(): void {
    this.botoncrear.set(false);
    this.botoneditar.set(false);
    this.cargarCarreras();
    this.auditRefresh++;
  }

  cargarCarreras(): void {
    this.carreraService.obtenerCarreras().subscribe({
      next: (data: Carrera[]) => {
        this.carreras = data;
        this.carrerascopia = [...this.carreras];
        this.aplicarFiltros();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar las carreras , intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }

  aplicarFiltros(event?: [string, string]): void {
    const busqueda = this.mantenerBusqueda(event);
    if (busqueda[0].trim() !== '') {
      const busquedaNormalizada = this.normalizeText(busqueda[0]);
      this.carreras = this.carrerascopia.filter((carrera) => {
        switch (busqueda[1]) {
          case 'Nombre':
            return this.normalizeText(carrera.Nombre || '').includes(
              busquedaNormalizada,
            );
          case 'Código':
            return this.normalizeText(carrera.Codigo || '').includes(
              busquedaNormalizada,
            );
          default:
            return this.normalizeText(carrera.Nombre || '').includes(
              busquedaNormalizada,
            );
        }
      });
    } else {
      this.carreras = [...this.carrerascopia];
    }
    this.aplicarOrdenActualSiExiste();
  }

  override sortTable(sort: { col: string; dir: 'asc' | 'desc' }): void {
    this.carreras = this.sortByColumn(this.carreras, sort, {
      Nombre: (carrera) => carrera.Nombre,
      Código: (carrera) => carrera.Codigo,
    });
  }

  limpiarBusqueda(): void {
    this.limpiarBusquedaPersistida();
    this.aplicarFiltros();
  }

  editarCarrera(carrera: Carrera): void {
    this.botoncrear.set(false);
    this.carreraSeleccionada = { ...carrera };
    this.botoneditar.set(true);
  }

  eliminarCarrera(carrera: Carrera): void {
    this.carreraSeleccionada = carrera;
    this.alertaeliminar = true;
  }

  confirmarEliminacion(): void {
    if (this.carreraSeleccionada.Id) {
      this.carreraService
        .eliminarCarrera(this.carreraSeleccionada.Id)
        .subscribe({
          next: () => {
            this.cargarCarreras();
            this.toast.success('Carrera eliminada correctamente.');
            this.auditRefresh++;
          },
          error: (error) => {
            const errorMsg = extractErrorMessage(
              error,
              'Error al eliminar la carrera',
            );
            this.mensajeerror = errorMsg;
            this.error.set(true);
          },
        });
    }
    this.limpiarCarreraSeleccionada();
    this.alertaeliminar = false;
  }

  cancelarEliminacion(): void {
    this.alertaeliminar = false;
    this.limpiarCarreraSeleccionada();
  }
}
