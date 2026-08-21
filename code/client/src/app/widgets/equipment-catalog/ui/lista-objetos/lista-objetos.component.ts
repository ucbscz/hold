import { CommonModule, NgOptimizedImage } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  signal,
  SimpleChanges,
  ViewChild,
  WritableSignal,
} from '@angular/core';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { DisponibilidadService } from '@entities/availability';
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import { CarritoService } from '@features/cart';
import { ImageCacheService } from '@shared/lib/image/image-cache.service';
import {
  EquipmentImagePlaceholderComponent,
  MostrarerrorComponent,
} from '@shared/ui';

const DEFAULT_PRODUCTS_PER_PAGE = 21;
const DESIRED_GRID_ROWS = 4;
const MIN_VISIBLE_PRODUCTS = 6;
const PAGINATION_BUTTON_COUNT = 5;
const NEXT_PAGE_PRELOAD_OFFSET = 1;
const DEFAULT_MAX_QUANTITY = 99;

@Component({
  selector: 'app-lista-objetos',
  standalone: true,
  imports: [
    RouterModule,
    MostrarerrorComponent,
    EquipmentImagePlaceholderComponent,
    CommonModule,
    NgOptimizedImage,
  ],
  templateUrl: './lista-objetos.component.html',
  styleUrl: './lista-objetos.component.css',
})
export class ListaObjetosComponent
  implements OnChanges, OnDestroy, OnInit, AfterViewInit
{
  @ViewChild('gridContainer') gridContainer?: ElementRef<HTMLElement>;
  @Input() categorias: string[] = [];
  @Input() producto: string = '';

  private todosLosProductos: GrupoEquipo[] = [];

  productosFiltrados: GrupoEquipo[] = [];
  productosPaginados: GrupoEquipo[][] = [];
  cantidadObjetos: number = DEFAULT_PRODUCTS_PER_PAGE;
  paginaActual: number = 0;
  totalPaginas: number = 0;
  error: WritableSignal<boolean> = signal(false);
  mensajeError: string = '';

  cantidades: Record<number, number> = {};
  addedToCart: Record<number, boolean> = {};
  totalOperativo: Record<number, number> = {};
  private readonly carritoSubscription: Subscription;

  constructor(
    private readonly servicio: GrupoequipoService,
    private readonly carrito: CarritoService,
    private readonly disponibilidad: DisponibilidadService,
    private readonly imageCache: ImageCacheService,
  ) {
    this.carritoSubscription = this.carrito.carrito$.subscribe(() =>
      this.sincronizarCarrito(),
    );
  }

  sinOperativos(id: number): boolean {
    return (this.totalOperativo[id] ?? 1) <= 0;
  }

  getCantidad(id: number): number {
    return this.cantidades[id] ?? 1;
  }

  incrementarCantidad(id: number, max: number, event: Event): void {
    this.detenerEvento(event);
    const c = this.getCantidad(id);

    if (c < (max || DEFAULT_MAX_QUANTITY)) {
      this.cantidades[id] = c + 1;
      this.actualizarCantidadEnCarrito(id);
    }
  }

  decrementarCantidad(id: number, event: Event): void {
    this.detenerEvento(event);
    const c = this.getCantidad(id);

    if (c > 1) {
      this.cantidades[id] = c - 1;
      this.actualizarCantidadEnCarrito(id);
    }
  }

  agregarAlCarrito(item: GrupoEquipo, event: Event): void {
    this.detenerEvento(event);

    if (this.sinOperativos(item.id!)) return;

    if (this.addedToCart[item.id!]) {
      this.carrito.eliminarProducto(item.id!);
      this.addedToCart[item.id!] = false;

      return;
    }

    this.carrito.establecerCantidad(
      item.id!,
      item.nombre,
      item.link ?? '',
      item.marca ?? '',
      item.modelo ?? '',
      item.CostoPromedio ?? 0,
      item.Cantidad ?? 1,
      this.getCantidad(item.id!),
      item.TiempoMaximoPrestamoDias,
    );
  }

  ngOnInit(): void {
    this.cantidadObjetos =
      this.servicio.cantidadObjetosGuardada || DEFAULT_PRODUCTS_PER_PAGE;
    this.paginaActual = this.servicio.paginaGuardada;
    this.cargarProductos();
  }

  private sincronizarCarrito(): void {
    const carrito = this.carrito.obtenerCarrito();
    Object.keys(this.addedToCart).forEach((id) => {
      this.addedToCart[Number(id)] = !!carrito[Number(id)];
    });
    Object.entries(carrito).forEach(([id, item]) => {
      this.addedToCart[Number(id)] = true;
      this.cantidades[Number(id)] = item.cantidad;
    });
  }

  private actualizarCantidadEnCarrito(id: number): void {
    if (!this.carrito.contieneProducto(id)) return;

    this.carrito.editarCantidad(id, this.getCantidad(id));
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.calcularCantidadObjetos());
  }

  @HostListener('window:resize')
  onResize(): void {
    this.calcularCantidadObjetos();
  }

  private calcularCantidadObjetos(): void {
    const element = this.gridContainer?.nativeElement;

    if (!element) return;

    const computedStyle = window.getComputedStyle(element);
    const gridColumns = computedStyle.getPropertyValue('grid-template-columns');

    let columnas = 1;
    if (gridColumns && gridColumns !== 'none') {
      columnas = gridColumns.trim().split(/\s+/).length;
    }

    let nuevaCantidad = columnas * DESIRED_GRID_ROWS;

    if (nuevaCantidad < MIN_VISIBLE_PRODUCTS) {
      nuevaCantidad = MIN_VISIBLE_PRODUCTS;
    }

    if (this.cantidadObjetos !== nuevaCantidad) {
      const primerItemVisible = this.paginaActual * this.cantidadObjetos;
      this.cantidadObjetos = nuevaCantidad;
      this.servicio.cantidadObjetosGuardada = nuevaCantidad;
      this.paginaActual = Math.floor(primerItemVisible / nuevaCantidad);
      this.servicio.paginaGuardada = this.paginaActual;

      if (this.todosLosProductos.length > 0) {
        this.filtrarProductos();
      }
    }
  }

  ngOnDestroy(): void {
    this.carritoSubscription.unsubscribe();
    this.servicio.paginaGuardada = this.paginaActual;
    this.servicio.cantidadObjetosGuardada = this.cantidadObjetos;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      (changes['categorias'] || changes['producto']) &&
      this.todosLosProductos.length > 0
    ) {
      this.filtrarProductos();
    }
  }

  private cargarProductos(): void {
    this.servicio.getGrupoEquipo('', '').subscribe({
      next: (data) => {
        this.todosLosProductos = data;
        this.addedToCart = Object.fromEntries(
          data
            .filter((item) => item.id != null)
            .map((item) => [item.id!, this.carrito.contieneProducto(item.id!)]),
        );
        this.filtrarProductos();
        this.cargarOperatividad(data);
      },
      error: () => {
        this.mensajeError =
          'No se pudo cargar el catálogo de equipos. Por favor, recarga la página e inténtalo de nuevo.';
        this.error.set(true);
      },
    });
  }

  private cargarOperatividad(productos: GrupoEquipo[]): void {
    const ids = productos
      .map((p) => p.id)
      .filter((id): id is number => id != null);

    if (ids.length === 0) return;

    const inicio = new Date();
    const fin = new Date(inicio.getTime() + 30 * 60 * 1000);

    this.disponibilidad.obtenerDisponibilidad(inicio, fin, ids).subscribe({
      next: (data) => {
        for (const d of data) {
          this.totalOperativo[d.IdGrupoEquipo] = d.TotalOperativo ?? 0;
        }
      },
    });
  }

  private normalizeText(text: unknown): string {
    return String(text || '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  private filtrarProductos(): void {
    let productos = [...this.todosLosProductos];

    if (this.categorias.length > 0) {
      if (this.categorias.includes('sinCategoria')) {
        productos = productos.filter(
          (p) => p.nombreCategoria === null || p.nombreCategoria === '',
        );
      } else {
        productos = productos.filter((p) =>
          this.categorias.includes(p.nombreCategoria || ''),
        );
      }
    }

    if (this.producto) {
      const busquedaNormalizada = this.normalizeText(this.producto);

      productos = productos.filter(
        (p) =>
          this.normalizeText(p.nombre || '').includes(busquedaNormalizada) ||
          this.normalizeText(p.modelo || '').includes(busquedaNormalizada) ||
          this.normalizeText(p.marca || '').includes(busquedaNormalizada),
      );
    }

    productos = productos.filter((p) => (p.Cantidad ?? 0) > 0);
    this.productosFiltrados = productos;
    this.productosPaginados = this.paginar(productos);
    this.totalPaginas = this.productosPaginados.length;

    if (this.paginaActual >= this.totalPaginas) {
      this.paginaActual = Math.max(0, this.totalPaginas - 1);
    }

    this.precargarImagenesCercanas();
  }

  paginar(productos: GrupoEquipo[]): GrupoEquipo[][] {
    const resultado: GrupoEquipo[][] = [];
    for (let i = 0; i < productos.length; i += this.cantidadObjetos) {
      resultado.push(productos.slice(i, i + this.cantidadObjetos));
    }
    return resultado;
  }

  obtenerRangoPaginas(): number[] {
    const maxBotones = PAGINATION_BUTTON_COUNT;
    if (this.totalPaginas <= maxBotones) {
      return Array.from({ length: this.totalPaginas }, (_, i) => i);
    }

    const rangoMedio = Math.floor(maxBotones / 2);
    let inicio = this.paginaActual - rangoMedio;

    if (inicio < 0) {
      inicio = 0;
    }

    if (inicio + maxBotones > this.totalPaginas) {
      inicio = this.totalPaginas - maxBotones;
    }

    return Array.from({ length: maxBotones }, (_, i) => inicio + i);
  }

  actualizarPagina(pagina: number): void {
    if (pagina >= 0 && pagina < this.totalPaginas) {
      this.paginaActual = pagina;
      this.servicio.paginaGuardada = pagina;
      this.precargarImagenesCercanas();
    }
  }

  get productosActuales(): GrupoEquipo[] {
    return this.productosPaginados[this.paginaActual] || [];
  }

  get hayPaginasAnteriores(): boolean {
    return this.paginaActual > 0;
  }

  get hayPaginasSiguientes(): boolean {
    return this.paginaActual < this.totalPaginas - 1;
  }

  private detenerEvento(event: Event): void {
    event.stopPropagation();
    event.preventDefault();
  }

  obtenerImagenEquipo(item: GrupoEquipo): string | null {
    const imageUrl = item.link?.trim();

    if (!imageUrl) return null;
    if (!this.imageCache.canDisplay(imageUrl)) return null;
    if (this.imageCache.hasFailed(imageUrl)) return null;

    return imageUrl;
  }

  imagenCargada(imageUrl: string | null | undefined): boolean {
    return this.imageCache.isLoaded(imageUrl);
  }

  registrarImagenCargada(imageUrl: string): void {
    this.imageCache.markLoaded(imageUrl);
  }

  registrarImagenFallida(imageUrl: string): void {
    this.imageCache.markFailed(imageUrl);
  }

  private precargarImagenesCercanas(): void {
    const paginasCercanas = [
      this.paginaActual,
      this.paginaActual + NEXT_PAGE_PRELOAD_OFFSET,
    ];
    const imageUrls = paginasCercanas.flatMap((pagina) =>
      (this.productosPaginados[pagina] ?? [])
        .map((producto) => producto.link)
        .filter((url) => this.imageCache.canDisplay(url)),
    );

    this.imageCache.preload(imageUrls);
  }
}
