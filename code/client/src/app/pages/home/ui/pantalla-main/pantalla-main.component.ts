import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Categorias } from '@entities/admin';
import { CategoriaService } from '@entities/category';
import { FiltrosService } from '@features/catalog-filter';
import { extractErrorMessage } from '@shared/lib/error';
import { MostrarerrorComponent } from '@shared/ui';
import { ListaObjetosComponent } from '@widgets/equipment-catalog';

@Component({
  selector: 'app-pantalla-main',
  standalone: true,
  imports: [
    CommonModule,
    ListaObjetosComponent,
    FormsModule,
    MostrarerrorComponent,
  ],
  templateUrl: './pantalla-main.component.html',
  styleUrl: './pantalla-main.component.css',
})
export class PantallaMainComponent implements OnInit, OnDestroy {
  showCategories = false;
  solicitud = '';
  categoriasSeleccionadas: Set<string> = new Set();
  categoriasArray: string[] = [];
  items: Categorias[] = [];
  error: WritableSignal<boolean> = signal(false);
  mensajeerror = '';

  constructor(
    private readonly categorias: CategoriaService,
    private readonly filtrosService: FiltrosService,
    private readonly elementRef: ElementRef<HTMLElement>,
  ) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (
      this.showCategories &&
      !this.elementRef.nativeElement.contains(event.target as Node)
    ) {
      this.showCategories = false;
    }
  }

  ngOnInit(): void {
    this.categoriasSeleccionadas = this.filtrosService.categoriasSeleccionadas;
    this.actualizarCategoriasArray();
    this.solicitud = this.filtrosService.solicitud;

    this.categorias.obtenercategorias().subscribe({
      next: (data) => (this.items = data),
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar las categorias , intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }

  ngOnDestroy(): void {
    this.filtrosService.solicitud = this.solicitud;
  }

  limpiar(): void {
    this.solicitud = '';
    this.categoriasSeleccionadas.clear();
    this.actualizarCategoriasArray();
    this.filtrosService.limpiar();
  }

  limpiarBusqueda(): void {
    this.solicitud = '';
    this.filtrosService.solicitud = '';
  }

  alternarCategorias(): void {
    this.showCategories = !this.showCategories;
  }

  ocultarCategorias(): void {
    this.showCategories = false;
  }

  seleccionarCategoria(categoria: string): void {
    if (this.categoriasSeleccionadas.has(categoria)) {
      this.categoriasSeleccionadas.delete(categoria);
    } else {
      if (categoria === '') {
        this.categoriasSeleccionadas.clear();
      } else {
        this.categoriasSeleccionadas.delete('');
      }
      this.categoriasSeleccionadas.add(categoria);
    }
    this.actualizarCategoriasArray();
  }

  estaCategoriaSeleccionada(categoria: string): boolean {
    return this.categoriasSeleccionadas.has(categoria);
  }

  private actualizarCategoriasArray(): void {
    this.categoriasArray = Array.from(this.categoriasSeleccionadas);
  }
}
