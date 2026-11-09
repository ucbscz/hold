import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  NgZone,
  OnChanges,
  OnDestroy,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import {
  ComentarioEquipo,
  ComentarioEquipoOrden,
  GrupoequipoService,
} from '@entities/equipment-group';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import {
  actualizarComentario,
  esOrdenComentarios,
  removerComentario,
  totalComentarios,
} from '../../model/comentarios';

const MAX_COMMENT_LENGTH = 1024;

@Component({
  selector: 'app-comentarios-equipo',
  standalone: true,
  imports: [CommonModule, ValidatedFormsModule, CustomSelectComponent],
  templateUrl: './comentarios-equipo.component.html',
  styleUrl: './comentarios-equipo.component.css',
})
export class ComentariosEquipoComponent
  implements OnChanges, AfterViewInit, OnDestroy
{
  private readonly servicio = inject(GrupoequipoService);
  private readonly ngZone = inject(NgZone);
  @Input({ required: true }) grupoId!: number;
  @ViewChild('comentariosSection')
  private comentariosSection?: ElementRef<HTMLElement>;

  readonly maxCommentLength = MAX_COMMENT_LENGTH;
  readonly skeletonComentarios = [0, 1, 2];
  readonly ordenComentariosOpciones: OpcionSelect[] = [
    { value: 'recientes', label: 'Más recientes' },
    { value: 'antiguos', label: 'Más antiguos' },
    { value: 'likes', label: 'Más likes' },
  ];
  readonly mostrarBotonSubir = signal(false);
  comentarios: ComentarioEquipo[] = [];
  comentarioTexto = '';
  comentarioError = '';
  ordenComentarios: ComentarioEquipoOrden = 'recientes';
  respuestaActivaId: number | null = null;
  respuestaTexto: Record<number, string | undefined> = {};
  cargandoComentarios = false;
  publicandoComentario = false;
  publicandoRespuestaId: number | null = null;
  eliminandoComentarioId: number | null = null;
  private readonly likePendienteIds = new Set<number>();
  private scrollAnimationFrame: number | null = null;
  private readonly actualizarBotonSubir = (): void => {
    if (this.scrollAnimationFrame !== null) return;

    this.scrollAnimationFrame = window.requestAnimationFrame(() => {
      this.scrollAnimationFrame = null;
      const rect =
        this.comentariosSection?.nativeElement.getBoundingClientRect();
      const visible = !!rect && rect.top < -240 && rect.bottom > 96;

      if (visible !== this.mostrarBotonSubir()) {
        this.ngZone.run(() => this.mostrarBotonSubir.set(visible));
      }
    });
  };

  ngOnChanges(): void {
    this.cargarComentarios();
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

  cargarComentarios(): void {
    if (!this.grupoId) return;

    this.cargandoComentarios = true;
    this.comentarioError = '';

    this.servicio
      .obtenerComentarios(this.grupoId, this.ordenComentarios)
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
    if (!esOrdenComentarios(orden) || this.ordenComentarios === orden) return;

    this.ordenComentarios = orden;
    this.cargarComentarios();
  }

  publicarComentario(): void {
    const contenido = this.validarContenido(this.comentarioTexto, 'comentario');
    if (!contenido) return;

    this.publicandoComentario = true;
    this.servicio.crearComentario(this.grupoId, contenido).subscribe({
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
    const contenido = this.validarContenido(
      this.respuestaTexto[comentarioId] ?? '',
      'respuesta',
    );
    if (!contenido) return;

    this.publicandoRespuestaId = comentarioId;
    this.servicio
      .crearComentario(this.grupoId, contenido, comentarioId)
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
    if (!this.grupoId || this.likePendienteIds.has(comentario.id)) return;

    this.likePendienteIds.add(comentario.id);
    this.comentarioError = '';

    this.servicio
      .alternarLikeComentario(this.grupoId, comentario.id)
      .subscribe({
        next: (actualizado) => {
          this.comentarios = actualizarComentario(
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
    if (!this.grupoId || this.eliminandoComentarioId === comentario.id) return;

    this.eliminandoComentarioId = comentario.id;
    this.comentarioError = '';

    this.servicio.eliminarComentario(this.grupoId, comentario.id).subscribe({
      next: () => {
        this.comentarios = removerComentario(this.comentarios, comentario.id);
        this.eliminandoComentarioId = null;
      },
      error: () => {
        this.comentarioError = 'No se pudo eliminar el comentario.';
        this.eliminandoComentarioId = null;
      },
    });
  }

  totalComentarios(): number {
    return totalComentarios(this.comentarios);
  }

  likePendiente(comentarioId: number): boolean {
    return this.likePendienteIds.has(comentarioId);
  }

  private validarContenido(
    texto: string,
    tipo: 'comentario' | 'respuesta',
  ): string | null {
    if (!this.grupoId) return null;

    const contenido = texto.trim();
    const articulo = tipo === 'comentario' ? 'El' : 'La';
    const indefinido = tipo === 'comentario' ? 'un' : 'una';

    this.comentarioError = !contenido
      ? `Escribe ${indefinido} ${tipo} antes de publicar.`
      : contenido.length > MAX_COMMENT_LENGTH
        ? `${articulo} ${tipo} no puede superar ${MAX_COMMENT_LENGTH} caracteres.`
        : '';

    return this.comentarioError ? null : contenido;
  }
}
