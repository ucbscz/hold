import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Componente } from '@entities/admin';
import { ComponenteService } from '@entities/component';
import { BuscadorComponent } from '@features/admin-search';
import { Tabla } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoEliminarComponent,
  AvisoExitoComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { ComponentesCrearComponent } from '../componentes-crear/componentes-crear.component';
import { ComponentesEditarComponent } from '../componentes-editar/componentes-editar.component';
@Component({
  selector: 'app-componentes-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ComponentesCrearComponent,
    ComponentesEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    AuditPanelComponent,
  ],
  templateUrl: './componentes-tabla.component.html',
  styleUrl: './componentes-tabla.component.css',
})
export class ComponentesTablaComponent extends Tabla implements OnInit {
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }

  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  componentes: Componente[] = [];
  componentescopia: Componente[] = [];
  componenteSeleccionado: Componente = new Componente();
  override columnas: string[] = [
    'Nombre',
    'Modelo',
    'Tipo',
    'Código IMT del Equipo',
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
          case 'Código IMT del Equipo':
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
      'Código IMT del Equipo': (componente) => componente.CodigoImtEquipo,
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
            this.mensajeexito = 'Componente eliminado exitosamente';
            this.exito.set(true);
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
