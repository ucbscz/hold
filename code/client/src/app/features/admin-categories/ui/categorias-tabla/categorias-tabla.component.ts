import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Categorias } from '@entities/admin';
import { CategoriaService } from '@entities/category';
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
import { CategoriasCrearComponent } from '../categorias-crear/categorias-crear.component';
import { CategoriasEditarComponent } from '../categorias-editar/categorias-editar.component';
@Component({
  selector: 'app-categorias-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    CategoriasCrearComponent,
    CategoriasEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    AuditPanelComponent,
  ],
  templateUrl: './categorias-tabla.component.html',
  styleUrl: './categorias-tabla.component.css',
})
export class CategoriasTablaComponent extends Tabla {
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number): void {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }

  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  categorias: Categorias[] = [];
  categoriascopia: Categorias[] = [];
  categoriaSeleccionada: Categorias = new Categorias();
  override columnas: string[] = ['Nombre'];

  constructor(private readonly categoriaService: CategoriaService) {
    super();
  }

  ngOnInit(): void {
    this.cargarCategorias();
  }

  limpiarCategoriaSeleccionada(): void {
    this.categoriaSeleccionada = new Categorias();
  }

  crearCategoria(): void {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }

  cargarCategorias(): void {
    this.categoriaService.obtenercategorias().subscribe({
      next: (data: Categorias[]) => {
        this.categorias = data;
        this.categoriascopia = [...this.categorias];
        this.aplicarFiltros();
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
  aplicarFiltros(event?: [string, string]): void {
    const busqueda = this.mantenerBusqueda(event);
    if (busqueda[0].trim() === '') {
      this.categorias = [...this.categoriascopia];
      return;
    }
    const busquedaNormalizada = this.normalizeText(busqueda[0]);
    this.categorias = this.categoriascopia.filter((categoria) =>
      this.normalizeText(categoria.Nombre || '').includes(busquedaNormalizada),
    );
  }

  limpiarBusqueda(): void {
    this.limpiarBusquedaPersistida();
    this.categorias = [...this.categoriascopia];
  }

  editarCategoria(categoria: Categorias): void {
    this.botoncrear.set(false);
    this.categoriaSeleccionada = { ...categoria };
    this.botoneditar.set(true);
  }

  eliminarCategoria(categoria: Categorias): void {
    this.categoriaSeleccionada = categoria;
    this.alertaeliminar = true;
  }

  confirmarEliminacion(): void {
    if (this.categoriaSeleccionada.Id) {
      this.categoriaService
        .eliminarCategoria(this.categoriaSeleccionada.Id)
        .subscribe({
          next: () => {
            this.cargarCategorias();
            this.mensajeexito = 'Categoría eliminada con éxito';
            this.exito.set(true);
            this.auditRefresh++;
          },
          error: (error) => {
            const errorMsg = extractErrorMessage(
              error,
              'Error al eliminar la categoría, intente más tarde',
            );
            this.mensajeerror = errorMsg;
            this.error.set(true);
          },
        });
    }
    this.limpiarCategoriaSeleccionada();
    this.alertaeliminar = false;
  }

  cancelarEliminacion(): void {
    this.alertaeliminar = false;
    this.limpiarCategoriaSeleccionada();
  }
}
