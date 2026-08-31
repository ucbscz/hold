import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  inject,
  Input,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ComponenteGrupo,
  GrupoEquipo,
  GrupoequipoService,
} from '@entities/equipment-group';
import { Subscription, finalize } from 'rxjs';

@Component({
  selector: 'app-inspeccion-equipo',
  standalone: true,
  imports: [CommonModule],
  template: `
    <dialog
      #modal
      (cancel)="$event.preventDefault(); cerrar.emit()"
      aria-labelledby="inspection-title"
    >
      <header>
        <h2 id="inspection-title">
          {{ grupo?.nombre || 'Características del equipo' }}
        </h2>
        <button
          class="modal-close-btn"
          (click)="cerrar.emit()"
          aria-label="Cerrar"
        >
          <i class="fas fa-times"></i>
        </button>
      </header>
      <div class="inspection-content">
        @if (error) {
          <p role="alert">{{ error }}</p>
        }
        @if (grupo) {
          @if (grupo.link) {
            <img
              [src]="grupo.link"
              alt=""
              width="180"
              height="140"
              (error)="$any($event.target).hidden = true"
            />
          }
          <dl>
            <div>
              <dt>Modelo</dt>
              <dd>{{ grupo.modelo || 'Sin registrar' }}</dd>
            </div>
            <div>
              <dt>Marca</dt>
              <dd>{{ grupo.marca || 'Sin registrar' }}</dd>
            </div>
            <div>
              <dt>Costo de referencia</dt>
              <dd>{{ grupo.CostoPromedio | number: '1.2-2' }} Bs</dd>
            </div>
            <div>
              <dt>Préstamo máximo</dt>
              <dd>{{ grupo.TiempoMaximoPrestamoDias }} días</dd>
            </div>
          </dl>
          <p>{{ grupo.descripcion }}</p>
        }
        <h3>Componentes</h3>
        @for (c of componentes; track c.Id) {
          <details>
            <summary>
              {{ c.Nombre }}
              <span>{{
                c.CodigoImtEquipo ? 'IMT ' + c.CodigoImtEquipo : ''
              }}</span>
            </summary>
            <p>{{ c.Modelo }} · {{ c.Tipo }}</p>
            <p>{{ c.Descripcion || 'Sin descripción' }}</p>
            <p>
              Costo de referencia: {{ c.PrecioReferencia | number: '1.2-2' }} Bs
            </p>
          </details>
        } @empty {
          @if (!cargando) {
            <p>No hay componentes registrados.</p>
          }
        }
        @if (cargando) {
          <p role="status">Cargando componentes...</p>
        }
        @if (hayMas && !cargando) {
          <button class="btn btn-secondary" (click)="cargarComponentes()">
            Ver más componentes
          </button>
        }
      </div>
    </dialog>
  `,
  styles: [
    `
      dialog[open] {
        display: flex;
        flex-direction: column;
      }
      header {
        flex: 0 0 auto;
      }
      .inspection-content {
        flex: 1;
        min-height: 0;
      }
      dialog {
        box-sizing: border-box;
        width: min(760px, calc(100vw - 24px));
        height: min(720px, calc(100dvh - 24px));
        max-height: calc(100dvh - 24px);
        padding: 0;
        border: 1px solid var(--border);
        border-radius: var(--radius-xl, 24px);
        background: var(--surface, #fff);
        color: var(--ink);
        font-family: var(--font);
        overflow: hidden;
      }
      dialog::backdrop {
        background: rgba(15, 23, 42, 0.45);
      }
      header {
        position: relative;
        padding: 24px 72px 20px 24px;
        border-bottom: 1px solid var(--border);
        min-height: 80px;
      }
      h2 {
        margin: 0;
        font-size: 22px;
        overflow-wrap: anywhere;
      }
      h3 {
        font-size: 18px;
      }
      .inspection-content {
        overflow: auto;
        padding: 24px;
        box-sizing: border-box;
      }
      img {
        object-fit: contain;
        display: block;
        margin: 0 auto 20px;
        max-width: 100%;
      }
      dl {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
      }
      dt {
        color: var(--ink-muted);
        font-size: 14px;
      }
      dd {
        margin: 4px 0;
        overflow-wrap: anywhere;
      }
      details {
        border-bottom: 1px solid var(--border);
        padding: 12px 0;
      }
      summary {
        cursor: pointer;
        min-height: 32px;
        overflow-wrap: anywhere;
      }
      summary span {
        font-size: 13px;
        color: var(--ink-secondary);
        margin-left: 8px;
      }
      p {
        line-height: 1.6;
        overflow-wrap: anywhere;
      }
      @media (max-width: 480px) {
        header {
          padding-left: 16px;
        }
        h2 {
          font-size: 19px;
        }
        .inspection-content {
          padding: 16px;
        }
        dl {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class InspeccionEquipoComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  @Input({ required: true }) grupoId!: number;
  @Output() cerrar = new EventEmitter<void>();
  @ViewChild('modal') modal!: ElementRef<HTMLDialogElement>;
  private readonly api = inject(GrupoequipoService);
  private readonly solicitudes = new Subscription();
  grupo?: GrupoEquipo;
  componentes: ComponenteGrupo[] = [];
  error = '';
  cargando = false;
  hayMas = true;
  private pagina = 1;
  ngOnInit() {
    this.solicitudes.add(
      this.api
        .getproducto(String(this.grupoId))
        .subscribe({
          next: (g) => (this.grupo = g),
          error: () => (this.error = 'No se pudo cargar el equipo.'),
        }),
    );
    this.cargarComponentes();
  }
  ngAfterViewInit() {
    this.modal.nativeElement.showModal();
  }
  cargarComponentes() {
    if (this.cargando) return;
    this.cargando = true;
    this.solicitudes.add(
      this.api
        .obtenerComponentes(this.grupoId, this.pagina)
        .pipe(finalize(() => (this.cargando = false)))
        .subscribe({
          next: (lista) => {
            this.componentes = [...this.componentes, ...lista];
            this.pagina++;
            this.hayMas = lista.length === 100;
          },
          error: () => {
            this.error = 'No se pudieron cargar los componentes.';
            this.hayMas = false;
          },
        }),
    );
  }
  ngOnDestroy() {
    this.solicitudes.unsubscribe();
    this.modal?.nativeElement.close();
  }
}
