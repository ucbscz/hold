import { INVENTORY_LABELS } from '@brand/brand';
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Componente, ComponenteService } from '@entities/component';
import {
  AvisoEliminarComponent,
  BuscadorComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { AdminTabsComponent, TablaGestion } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import { AuditPanelComponent } from '@entities/audit-log';
import { ComponentesFormularioComponent } from '../componentes-formulario/componentes-formulario.component';
@Component({
  selector: 'app-componentes-tabla',
  standalone: true,
  imports: [
    AdminTabsComponent,
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ComponentesFormularioComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    BuscadorComponent,
    AuditPanelComponent,
  ],
  templateUrl: './componentes-tabla.component.html',
  styleUrls: [
    '../../../../shared/lib/admin-table/tabla-admin.css',
    './componentes-tabla.component.css',
  ],
})
export class ComponentesTablaComponent extends TablaGestion implements OnInit {
  componentes: Componente[] = [];
  componentescopia: Componente[] = [];
  componenteSeleccionado: Componente = new Componente();
  override columnas: string[] = [
    'Nombre',
    'Modelo',
    'Tipo',
    INVENTORY_LABELS.equipmentInternalCode,
    'Precio Referencia',
  ];
  constructor(private readonly componenteService: ComponenteService) {
    super();
  }
  ngOnInit() {
    this.cargarComponentes();
  }
  limpiarComponenteSeleccionado() {
    this.componenteSeleccionado = new Componente();
  }
  crearComponente() {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }
  actualizarDatos(): void {
    this.botoncrear.set(false);
    this.botoneditar.set(false);
    this.cargarComponentes();
    this.auditRefresh++;
  }

  cargarComponentes() {
    this.componenteService.obtenerComponentes().subscribe({
      next: (data: Componente[]) => {
        this.componentes = data;
        this.componentescopia = [...this.componentes];
        this.aplicarFiltros();
        this.aplicarOrdenActualSiExiste();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los componentes , Intente mas tarde',
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
      this.componentes = this.componentescopia.filter((componente) => {
        switch (busqueda[1]) {
          case 'Nombre':
            return this.normalizeText(componente.Nombre || '').includes(
              busquedaNormalizada,
            );
          case 'Modelo':
            return this.normalizeText(componente.Modelo || '').includes(
              busquedaNormalizada,
            );
          case 'Tipo':
            return this.normalizeText(componente.Tipo || '').includes(
              busquedaNormalizada,
            );
          case INVENTORY_LABELS.equipmentInternalCode:
            return this.normalizeText(
              String(componente.CodigoImtEquipo || ''),
            ).includes(busquedaNormalizada);
          case 'Precio Referencia':
            return this.normalizeText(
              String(componente.PrecioReferencia || ''),
            ).includes(busquedaNormalizada);
          default:
            return (
              this.normalizeText(componente.Nombre || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(componente.Modelo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(componente.Tipo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(
                String(componente.CodigoImtEquipo || ''),
              ).includes(busquedaNormalizada) ||
              this.normalizeText(componente.NombreEquipo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(
                String(componente.PrecioReferencia || ''),
              ).includes(busquedaNormalizada)
            );
        }
      });
    } else {
      this.componentes = [...this.componentescopia];
    }
    this.aplicarOrdenActualSiExiste();
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.componentes = this.sortByColumn(this.componentes, e, {
      Nombre: (componente) => componente.Nombre,
      Modelo: (componente) => componente.Modelo,
      Tipo: (componente) => componente.Tipo,
      [INVENTORY_LABELS.equipmentInternalCode]: (componente) =>
        componente.CodigoImtEquipo,
      'Precio Referencia': (componente) => componente.PrecioReferencia,
    });
  }
  limpiarBusqueda() {
    this.limpiarBusquedaPersistida();
    this.componentes = [...this.componentescopia];
    this.aplicarOrdenActualSiExiste();
  }
  editarComponente(componente: Componente) {
    this.botoncrear.set(false);
    this.componenteSeleccionado = { ...componente };
    this.botoneditar.set(true);
  }
  eliminarComponente(componente: Componente) {
    this.componenteSeleccionado = componente;
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    if (this.componenteSeleccionado.Id) {
      this.componenteService
        .eliminarComponente(this.componenteSeleccionado.Id)
        .subscribe({
          next: (_response) => {
            this.cargarComponentes();
            this.toast.success('Componente eliminado exitosamente.');
            this.auditRefresh++;
          },
          error: (error) => {
            const errorMsg = extractErrorMessage(
              error,
              'Error al eliminar el componente, intente más tarde',
            );
            this.mensajeerror = errorMsg;
            this.error.set(true);
          },
        });
    }
    this.limpiarComponenteSeleccionado();
    this.alertaeliminar = false;
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.limpiarComponenteSeleccionado();
  }
}
