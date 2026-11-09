import { CommonModule } from '@angular/common';
import { InspeccionEquipoComponent } from '@features/equipment-inspection';
import { Component, OnDestroy, signal, WritableSignal } from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { AvisoDisponibilidadService } from '@entities/availability';
import { Carrito } from '@entities/cart';
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import { CalendarioComponent } from '@features/availability-selector';
import { ComentariosEquipoComponent } from '@features/equipment-comments';
import { CarritoService } from '@features/cart';
import { ImageCacheService } from '@shared/lib/image/image-cache.service';
import {
  EquipmentImagePlaceholderComponent,
  MostrarerrorComponent,
} from '@shared/ui';

const MINIMUM_CART_QUANTITY = 1;
const FALLBACK_MAXIMUM_QUANTITY = 99;

@Component({
  selector: 'app-objeto',
  standalone: true,
  imports: [
    InspeccionEquipoComponent,
    CommonModule,
    MostrarerrorComponent,
    EquipmentImagePlaceholderComponent,
    CalendarioComponent,
    ValidatedFormsModule,
    ComentariosEquipoComponent,
  ],
  templateUrl: './objeto.component.html',
  styleUrl: './objeto.component.css',
})
export class ObjetoComponent implements OnDestroy {
  readonly minimumCartQuantity = MINIMUM_CART_QUANTITY;
  id: string = '';
  producto: GrupoEquipo = new GrupoEquipo();
  cantidadDisponible: number = 0;
  cargando: boolean = true;
  addedToCart = false;
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = '';
  deshabilitarBoton = false;

  cantidad: number = 1;

  showCalendarioModal = false;
  mostrarComponentes = false;
  carritoCalendario: Carrito = {};
  fechaInicioCalendario: WritableSignal<Date | null> = signal(null);
  fechaFinCalendario: WritableSignal<Date | null> = signal(null);

  showAvisoModal = false;
  avisoFecha: Date | null = null;
  avisoRegistrado = false;
  private readonly carritoSubscription: Subscription;
  constructor(
    private readonly route: ActivatedRoute,
    private readonly servicio: GrupoequipoService,
    private readonly carrito: CarritoService,
    private readonly avisoDisponibilidad: AvisoDisponibilidadService,
    private readonly imageCache: ImageCacheService,
  ) {
    this.carritoSubscription = this.carrito.carrito$.subscribe(() => {
      if (!this.producto.id) return;
      const item = this.carrito.obtenerCarrito()[this.producto.id];
      this.addedToCart = !!item;
      if (item) this.cantidad = item.cantidad;
    });
  }

  ngOnDestroy(): void {
    this.carritoSubscription.unsubscribe();
  }

  ngOnInit(): void {
    const routeId = this.route.snapshot.paramMap.get('id');
    if (!routeId) return;
    this.id = routeId;
    this.servicio.getproducto(routeId).subscribe({
      next: (data) => {
        this.producto = data;

        if (this.producto?.id) {
          this.addedToCart = this.carrito.contieneProducto(this.producto.id);
          const itemEnCarrito = this.carrito.obtenerCarrito()[this.producto.id];
          if (itemEnCarrito) this.cantidad = itemEnCarrito.cantidad;
          this.carritoCalendario = {
            [this.producto.id]: {
              nombre: this.producto.nombre ?? '',
              modelo: this.producto.modelo ?? '',
              marca: this.producto.marca ?? '',
              cantidad: 1,
              fecha_inicio: null,
              fecha_final: null,
              imagen: this.producto.link ?? '',
              precio: this.producto.CostoPromedio ?? 0,
              cantidadMax: this.producto.Cantidad ?? MINIMUM_CART_QUANTITY,
              tiempoMaximoPrestamoDias: this.producto.TiempoMaximoPrestamoDias,
            },
          };
          this.cantidadDisponible =
            this.producto.Cantidad ?? FALLBACK_MAXIMUM_QUANTITY;
          this.cargando = false;
        } else {
          this.cargando = false;
        }
      },
      error: () => {
        this.deshabilitarBoton = true;
        this.mensajeerror =
          'No se pudo cargar la información del equipo. Por favor, intenta más tarde.';
        this.cargando = false;
        this.error.set(true);
      },
    });
  }

  incrementar(): void {
    if (
      this.cantidad <
      Math.min(
        this.cantidadDisponible,
        this.producto.Cantidad ?? FALLBACK_MAXIMUM_QUANTITY,
      )
    ) {
      this.cantidad++;
      this.actualizarCantidadEnCarrito();
    }
  }

  decrementar(): void {
    if (this.cantidad > MINIMUM_CART_QUANTITY) {
      this.cantidad--;
      this.actualizarCantidadEnCarrito();
    }
  }

  abrirCalendarioModal(): void {
    this.showCalendarioModal = true;
  }

  cerrarCalendarioModal(): void {
    this.showCalendarioModal = false;
  }

  cerrarCalendarioDesdeFondo(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.cerrarCalendarioModal();
  }

  onAvisarDisponibilidad(fecha: Date): void {
    this.avisoFecha = fecha;
    this.avisoRegistrado = false;
    this.showAvisoModal = true;
  }

  confirmarAviso(): void {
    if (!this.producto.id || !this.avisoFecha) return;

    this.avisoDisponibilidad
      .registrar(this.producto.id, this.avisoFecha)
      .subscribe({
        next: () => (this.avisoRegistrado = true),
        error: () => {
          this.mensajeerror = 'No se pudo registrar el aviso.';
          this.error.set(true);
          this.showAvisoModal = false;
        },
      });
  }

  cerrarAvisoModal(): void {
    this.showAvisoModal = false;
    this.avisoRegistrado = false;
  }

  ocultarImagenProducto(): void {
    if (this.producto.link) this.imageCache.markFailed(this.producto.link);
    this.producto.link = null;
  }

  obtenerImagenProducto(): string | null {
    const imageUrl = this.producto.link?.trim();

    if (!imageUrl) return null;
    if (!this.imageCache.canDisplay(imageUrl)) return null;
    if (this.imageCache.hasFailed(imageUrl)) return null;

    return imageUrl;
  }

  detenerPropagacion(event: Event): void {
    event.stopPropagation();
  }

  alternarProductoCarrito(): void {
    if (this.addedToCart) {
      this.carrito.eliminarProducto(this.producto.id);
      this.addedToCart = false;

      return;
    }

    this.carrito.establecerCantidad(
      this.producto.id,
      this.producto.nombre,
      this.producto.link ?? '',
      this.producto.marca ?? '',
      this.producto.modelo ?? '',
      this.producto.CostoPromedio ?? 0,
      this.producto.Cantidad ?? MINIMUM_CART_QUANTITY,
      this.cantidad,
      this.producto.TiempoMaximoPrestamoDias,
    );
  }

  private actualizarCantidadEnCarrito(): void {
    if (!this.producto.id || !this.carrito.contieneProducto(this.producto.id))
      return;

    this.carrito.editarCantidad(this.producto.id, this.cantidad);
  }
}
