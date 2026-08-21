import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Categorias } from '@entities/admin';
import { CategoriaService } from '@entities/category';
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import { BuscadorComponent } from '@features/admin-search';
import { Tabla } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoEliminarComponent,
  AvisoExitoComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { EquiposInlineComponent } from '@widgets/admin-inline';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { GruposEquiposCrearComponent } from '../grupos-equipos-crear/grupos-equipos-crear.component';
import { GruposEquiposEditarComponent } from '../grupos-equipos-editar/grupos-equipos-editar.component';
@Component({
  selector: 'app-grupos-equipos-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    GruposEquiposCrearComponent,
    GruposEquiposEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    EquiposInlineComponent,
    AuditPanelComponent,
  ],
  templateUrl: './grupos-equipos-tabla.component.html',
  styleUrl: './grupos-equipos-tabla.component.css',
})
export class GruposEquiposTablaComponent extends Tabla implements OnInit {
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }
  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  gruposEquipos: GrupoEquipo[] = [];
  gruposEquiposFiltrados: GrupoEquipo[] = [];
  categorias: string[] = [];
  grupoEquipoSeleccionado: GrupoEquipo = new GrupoEquipo();
  override columnas: string[] = [
    'Nombre',
    'Cantidad',
    'Modelo',
    'Marca',
    'Categoría',
    'Descripción',
  ];
  constructor(
    private readonly grupoequipoapi: GrupoequipoService,
    private categoriasAPI: CategoriaService,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarGruposEquipos();
    this.obtenerCategorias();
  }
  obtenerCategorias() {
    this.categoriasAPI.obtenercategorias().subscribe({
      next: (data: Categorias[]) => {
        this.categorias = data.map((categoria) => categoria.Nombre ?? '');
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar las categorías, intente más tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  limpiarGrupoEquipoSeleccionado() {
    this.grupoEquipoSeleccionado = new GrupoEquipo();
  }
  creargrupoequipo() {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }
  cargarGruposEquipos() {
    this.grupoequipoapi.getGrupoEquipo('', '').subscribe({
      next: (data: GrupoEquipo[]) => {
        this.gruposEquipos = data;
        this.gruposEquiposFiltrados = [...this.gruposEquipos];
        this.aplicarFiltros();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los grupos de equipos, intente más tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  buscar() {
    this.aplicarFiltros();
  }
  aplicarFiltros(event?: [string, string]) {
    const busqueda = this.mantenerBusqueda(event);
    if (busqueda[0].trim() !== '') {
      const busquedaNormalizada = this.normalizeText(busqueda[0]);
      this.gruposEquiposFiltrados = this.gruposEquipos.filter((grupoequipo) => {
        switch (busqueda[1]) {
          case 'Nombre':
            return this.normalizeText(grupoequipo.nombre || '').includes(
              busquedaNormalizada,
            );
          case 'Cantidad':
            return this.normalizeText(
              String(grupoequipo.Cantidad || ''),
            ).includes(busquedaNormalizada);
          case 'Modelo':
            return this.normalizeText(grupoequipo.modelo || '').includes(
              busquedaNormalizada,
            );
          case 'Marca':
            return this.normalizeText(grupoequipo.marca || '').includes(
              busquedaNormalizada,
            );
          case 'Categoría':
            return this.normalizeText(
              grupoequipo.nombreCategoria || '',
            ).includes(busquedaNormalizada);
          case 'Descripción':
            return this.normalizeText(grupoequipo.descripcion || '').includes(
              busquedaNormalizada,
            );
          default:
            return (
              this.normalizeText(grupoequipo.nombre || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(String(grupoequipo.Cantidad || '')).includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(grupoequipo.modelo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(grupoequipo.marca || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(grupoequipo.nombreCategoria || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(grupoequipo.descripcion || '').includes(
                busquedaNormalizada,
              )
            );
        }
      });
    } else {
      this.gruposEquiposFiltrados = [...this.gruposEquipos];
    }
    this.aplicarOrdenActualSiExiste();
  }
  limpiarBusqueda() {
    this.limpiarBusquedaPersistida();
    this.gruposEquiposFiltrados = [...this.gruposEquipos];
    this.aplicarOrdenActualSiExiste();
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.gruposEquiposFiltrados = this.sortByColumn(
      this.gruposEquiposFiltrados,
      e,
      {
        Nombre: (grupoEquipo) => grupoEquipo.nombre,
        Cantidad: (grupoEquipo) => grupoEquipo.Cantidad,
        Modelo: (grupoEquipo) => grupoEquipo.modelo,
        Marca: (grupoEquipo) => grupoEquipo.marca,
        Categoría: (grupoEquipo) => grupoEquipo.nombreCategoria,
        Descripción: (grupoEquipo) => grupoEquipo.descripcion,
      },
    );
  }

  editarGrupoEquipo(grupoequipo: GrupoEquipo) {
    this.botoncrear.set(false);
    this.grupoEquipoSeleccionado = { ...grupoequipo };
    this.botoneditar.set(true);
  }
  eliminarGrupoEquipo(grupoequipo: GrupoEquipo) {
    this.grupoEquipoSeleccionado = grupoequipo;
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    this.grupoequipoapi
      .eliminarGrupoEquipo(this.grupoEquipoSeleccionado.id)
      .subscribe({
        next: (_response) => {
          this.mensajeexito = 'Grupo de equipo eliminado exitosamente';
          this.exito.set(true);
          this.auditRefresh++;
          this.cargarGruposEquipos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al eliminar el grupo de equipo, intente más tarde',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
    this.limpiarGrupoEquipoSeleccionado();
    this.alertaeliminar = false;
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.limpiarGrupoEquipoSeleccionado();
  }
}
