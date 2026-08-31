import { CommonModule } from '@angular/common';
import { InspeccionEquipoComponent } from '@features/equipment-inspection';
import {
  AfterViewInit,
  Component,
  ElementRef,
  NgZone,
  OnDestroy,
  signal,
  ViewChild,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import {
  AvisoDisponibilidadService,
  DisponibilidadService,
} from '@entities/availability';
import { Carrito } from '@entities/cart';
import {
  ComentarioEquipo,
  ComentarioEquipoOrden,
  GrupoEquipo,
  GrupoequipoService,
} from '@entities/equipment-group';
import { CalendarioComponent } from '@features/availability-selector';
import { CarritoService } from '@features/cart';
import { ImageCacheService } from '@shared/lib/image/image-cache.service';
import {
  EquipmentImagePlaceholderComponent,
  MostrarerrorComponent,
  CustomSelectComponent,
  OpcionSelect,
} from '@shared/ui';

const MINIMUM_CART_QUANTITY = 1;
const FALLBACK_MAXIMUM_QUANTITY = 99;
const MAX_COMMENT_LENGTH = 1024;

@Component({
  selector: 'app-objeto',
  standalone: true,
  imports: [
    InspeccionEquipoComponent,
    CommonModule,
    MostrarerrorComponent,
    EquipmentImagePlaceholderComponent,
    CalendarioComponent,
    FormsModule,
    CustomSelectComponent,
  ],
  templateUrl: './objeto.component.html',
  styleUrl: './objeto.component.css',
})
export class ObjetoComponent implements AfterViewInit, OnDestroy {
  @ViewChild('comentariosSection')
  private comentariosSection?: ElementRef<HTMLElement>;

  readonly minimumCartQuantity = MINIMUM_CART_QUANTITY;
  readonly maxCommentLength = MAX_COMMENT_LENGTH;
  readonly skeletonComentarios = [0, 1, 2];
  id: string = '';
  producto: GrupoEquipo = new GrupoEquipo();
  cantidadDisponible: number = 0;
  totalOperativo: number = 0;
  cargando: boolean = true;
  consultandoDisponibilidad = false;
  addedToCart = false;
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = '';
  deshabilitarBoton = false;
  sinUnidadesOperativas: boolean = false;

  cantidad: number = 1;

  showCalendarioModal = false;
  mostrarComponentes = false;
  carritoCalendario: Carrito = {};
  fechaInicioCalendario: WritableSignal<Date | null> = signal(null);
  fechaFinCalendario: WritableSignal<Date | null> = signal(null);

  showAvisoModal = false;
  avisoFecha: Date | null = null;
  avisoRegistrado = false;
  comentarios: ComentarioEquipo[] = [];
  comentarioTexto = '';
  comentarioError = '';
  ordenComentarios: ComentarioEquipoOrden = 'recientes';
  ordenComentariosOpciones: OpcionSelect[] = [
    { value: 'recientes', label: 'Más recientes' },
    { value: 'antiguos', label: 'Más antiguos' },
    { value: 'likes', label: 'Más likes' },
  ];
  respuestaActivaId: number | null = null;
  respuestaTexto: Record<number, string | undefined> = {};
  cargandoComentarios = false;
  publicandoComentario = false;
  publicandoRespuestaId: number | null = null;
  eliminandoComentarioId: number | null = null;
  readonly mostrarBotonSubir = signal(false);
  private readonly likePendienteIds = new Set<number>();
  private readonly carritoSubscription: Subscription;
  private scrollAnimationFrame: number | null = null;
  private readonly actualizarBotonSubir = (): void => {
    if (this.scrollAnimationFrame !== null) return;

    this.scrollAnimationFrame = window.requestAnimationFrame(() => {
      this.scrollAnimationFrame = null;
      const section = this.comentariosSection?.nativeElement;
      const rect = section?.getBoundingClientRect();
      const visible = !!rect && rect.top < -240 && rect.bottom > 96;

      if (visible !== this.mostrarBotonSubir()) {
        this.ngZone.run(() => this.mostrarBotonSubir.set(visible));
      }
    });
  };

  constructor(
    private readonly route: ActivatedRoute,
    private readonly servicio: GrupoequipoService,
    private readonly carrito: CarritoService,
    private readonly disponibilidadService: DisponibilidadService,
    private readonly avisoDisponibilidad: AvisoDisponibilidadService,
    private readonly imageCache: ImageCacheService,
    private readonly ngZone: NgZone,
  ) {
    this.carritoSubscription = this.carrito.carrito$.subscribe(() => {
      if (!this.producto.id) return;
      const item = this.carrito.obtenerCarrito()[this.producto.id];
      this.addedToCart = !!item;
      if (item) this.cantidad = item.cantidad;
    });
  }

  ngAfterViewInit(): void {
    if (typeof window === 'undefined') return;

    this.ngZone.runOutsideAngular(() => {
      window.addEventListener('scroll', this.actualizarBotonSubir, {
        passive: true,
      });
    });
    this.actualizarBotonSubir();
  }

  ngOnDestroy(): void {
    this.carritoSubscription.unsubscribe();
    if (typeof window === 'undefined') return;

    window.removeEventListener('scroll', this.actualizarBotonSubir);
    if (this.scrollAnimationFrame !== null) {
      window.cancelAnimationFrame(this.scrollAnimationFrame);
    }
  }

  subirAComentarios(): void {
    const reducirMovimiento = window.matchMedia(
      '(prefers-reduced-motion: reduce)',
    ).matches;
    this.comentariosSection?.nativeElement.scrollIntoView({
      behavior: reducirMovimiento ? 'auto' : 'smooth',
      block: 'start',
    });
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
          this.cargando = false;
          this.obtenerDisponibilidad();
          this.cargarComentarios();
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

  obtenerDisponibilidad(): void {
    const inicio = this.siguienteHorarioConsultable();
    const fin = new Date(inicio.getTime() + 30 * 60 * 1000);
    this.consultandoDisponibilidad = true;
    this.disponibilidadService
      .obtenerDisponibilidad(inicio, fin, [this.producto.id])
      .subscribe({
        next: (data) => {
          if (data?.length > 0) {
            this.cantidadDisponible = data[0].CantidadDisponible;
            this.totalOperativo = data[0].TotalOperativo ?? 0;
          }

          if (this.totalOperativo === 0) {
            this.sinUnidadesOperativas = true;
            this.deshabilitarBoton = true;
          }

          this.consultandoDisponibilidad = false;
        },
        error: () => {
          this.mensajeerror =
            'No se pudo obtener la disponibilidad del equipo. Por favor, intenta más tarde.';
          this.error.set(true);
          this.consultandoDisponibilidad = false;
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

  cargarComentarios(): void {
    if (!this.producto.id) return;

    this.cargandoComentarios = true;
    this.comentarioError = '';

    this.servicio
      .obtenerComentarios(this.producto.id, this.ordenComentarios)
      .subscribe({
        next: (comentarios) => {
          this.comentarios = comentarios;
          this.cargandoComentarios = false;
        },
        error: () => {
          this.comentarioError = 'No se pudieron cargar los comentarios.';
          this.cargandoComentarios = false;
        },
      });
  }

  cambiarOrdenComentarios(orden: unknown): void {
    if (!this.esOrdenComentarios(orden)) return;
    if (this.ordenComentarios === orden) return;

    this.ordenComentarios = orden;
    this.cargarComentarios();
  }

  publicarComentario(): void {
    if (!this.producto.id) return;

    const contenido = this.comentarioTexto.trim();

    if (!contenido) {
      this.comentarioError = 'Escribe un comentario antes de publicar.';
      return;
    }

    if (contenido.length > MAX_COMMENT_LENGTH) {
      this.comentarioError = `El comentario no puede superar ${MAX_COMMENT_LENGTH} caracteres.`;
      return;
    }

    this.publicandoComentario = true;
    this.comentarioError = '';

    this.servicio.crearComentario(this.producto.id, contenido).subscribe({
      next: () => {
        this.comentarioTexto = '';
        this.publicandoComentario = false;
        this.cargarComentarios();
      },
      error: () => {
        this.comentarioError = 'No se pudo publicar el comentario.';
        this.publicandoComentario = false;
      },
    });
  }

  alternarRespuesta(comentarioId: number): void {
    this.respuestaActivaId =
      this.respuestaActivaId === comentarioId ? null : comentarioId;
    this.comentarioError = '';
  }

  cancelarRespuesta(comentarioId: number): void {
    this.respuestaTexto[comentarioId] = '';
    this.respuestaActivaId = null;
  }

  publicarRespuesta(comentarioId: number): void {
    if (!this.producto.id) return;

    const contenido = (this.respuestaTexto[comentarioId] ?? '').trim();

    if (!contenido) {
      this.comentarioError = 'Escribe una respuesta antes de publicar.';
      return;
    }

    if (contenido.length > MAX_COMMENT_LENGTH) {
      this.comentarioError = `La respuesta no puede superar ${MAX_COMMENT_LENGTH} caracteres.`;
      return;
    }

    this.publicandoRespuestaId = comentarioId;
    this.comentarioError = '';

    this.servicio
      .crearComentario(this.producto.id, contenido, comentarioId)
      .subscribe({
        next: () => {
          this.respuestaTexto[comentarioId] = '';
          this.respuestaActivaId = null;
          this.publicandoRespuestaId = null;
          this.cargarComentarios();
        },
        error: () => {
          this.comentarioError = 'No se pudo publicar la respuesta.';
          this.publicandoRespuestaId = null;
        },
      });
  }

  alternarLikeComentario(comentario: ComentarioEquipo): void {
    if (!this.producto.id || this.likePendienteIds.has(comentario.id)) return;

    this.likePendienteIds.add(comentario.id);
    this.comentarioError = '';

    this.servicio
      .alternarLikeComentario(this.producto.id, comentario.id)
      .subscribe({
        next: (actualizado) => {
          this.comentarios = this.actualizarComentario(
            this.comentarios,
            actualizado,
          );
          this.likePendienteIds.delete(comentario.id);
        },
        error: () => {
          this.comentarioError = 'No se pudo actualizar el like.';
          this.likePendienteIds.delete(comentario.id);
        },
      });
  }

  eliminarComentario(comentario: ComentarioEquipo): void {
    if (!this.producto.id || this.eliminandoComentarioId === comentario.id)
      return;

    this.eliminandoComentarioId = comentario.id;
    this.comentarioError = '';

    this.servicio
      .eliminarComentario(this.producto.id, comentario.id)
      .subscribe({
        next: () => {
          this.comentarios = this.removerComentario(
            this.comentarios,
            comentario.id,
          );
          this.eliminandoComentarioId = null;
        },
        error: () => {
          this.comentarioError = 'No se pudo eliminar el comentario.';
          this.eliminandoComentarioId = null;
        },
      });
  }

  totalComentarios(): number {
    return this.comentarios.reduce(
      (total, comentario) => total + 1 + comentario.respuestas.length,
      0,
    );
  }

  likePendiente(comentarioId: number): boolean {
    return this.likePendienteIds.has(comentarioId);
  }

  private actualizarComentario(
    comentarios: ComentarioEquipo[],
    actualizado: ComentarioEquipo,
  ): ComentarioEquipo[] {
    return comentarios.map((comentario) =>
      comentario.id === actualizado.id
        ? {
            ...comentario,
            likes: actualizado.likes,
            likedByCurrentUser: actualizado.likedByCurrentUser,
            puedeEliminar: actualizado.puedeEliminar,
          }
        : {
            ...comentario,
            respuestas: this.actualizarComentario(
              comentario.respuestas,
              actualizado,
            ),
          },
    );
  }

  private removerComentario(
    comentarios: ComentarioEquipo[],
    comentarioId: number,
  ): ComentarioEquipo[] {
    return comentarios
      .filter((comentario) => comentario.id !== comentarioId)
      .map((comentario) => ({
        ...comentario,
        respuestas: this.removerComentario(comentario.respuestas, comentarioId),
      }));
  }

  private esOrdenComentarios(orden: unknown): orden is ComentarioEquipoOrden {
    return orden === 'recientes' || orden === 'antiguos' || orden === 'likes';
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

  private siguienteHorarioConsultable(): Date {
    const inicio = new Date();
    inicio.setSeconds(0, 0);

    if (inicio.getDay() === 0) {
      inicio.setDate(inicio.getDate() + 1);
      inicio.setHours(8, 0, 0, 0);
      return inicio;
    }

    if (inicio.getHours() < 8) {
      inicio.setHours(8, 0, 0, 0);
      return inicio;
    }

    if (inicio.getHours() >= 18) {
      inicio.setDate(inicio.getDate() + 1);
      if (inicio.getDay() === 0) inicio.setDate(inicio.getDate() + 1);
      inicio.setHours(8, 0, 0, 0);
      return inicio;
    }

    inicio.setMinutes(Math.ceil((inicio.getMinutes() + 1) / 30) * 30);
    if (inicio.getHours() >= 18) {
      inicio.setDate(inicio.getDate() + 1);
      if (inicio.getDay() === 0) inicio.setDate(inicio.getDate() + 1);
      inicio.setHours(8, 0, 0, 0);
    }
    return inicio;
  }
}
