import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrestamoDto } from '@entities/admin';
import { compararPrestamos } from '../../model/prioridad-prestamo';

export interface AccionTablero {
  id: number;
  accion: string;
}

@Component({
  selector: 'app-tablero-prestamos',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="loan-board" aria-label="Tablero de préstamos">
      @for (columna of columnas; track columna.estado) {
        <section class="loan-board__column" [attr.aria-label]="columna.nombre">
          <h2>
            {{ columna.nombre }} <span>{{ columna.filas.length }}</span>
          </h2>
          <div class="loan-board__items">
            @for (grupo of columna.filas; track grupo[0].Id) {
              <article class="loan-board__card">
                <div class="loan-board__meta">
                  <span>#{{ grupo[0].Id }}</span
                  ><span>{{ grupo[0].EstadoPrestamo | titlecase }}</span>
                </div>
                <button class="loan-board__title" (click)="detalle.emit(grupo)">
                  {{ grupo[0].NombreGrupoEquipo || 'Préstamo'
                  }}{{ grupo.length > 1 ? ' +' + (grupo.length - 1) : '' }}
                </button>
                @if (admin) {
                  <p>
                    {{ grupo[0].NombreUsuario }}
                    {{ grupo[0].ApellidoPaternoUsuario }}
                  </p>
                }
                <p>
                  {{
                    grupo[0].FechaPrestamoEsperada
                      | date: 'dd/MM/yyyy HH:mm' : '-0400'
                  }}<br />{{
                    grupo[0].FechaDevolucionEsperada
                      | date: 'dd/MM/yyyy HH:mm' : '-0400'
                  }}
                </p>
                <div class="loan-board__actions">
                  <button
                    class="btn btn-secondary"
                    (click)="detalle.emit(grupo)"
                  >
                    <i class="fas fa-eye"></i> Detalles
                  </button>
                  @if (grupo[0].IdContrato) {
                    <button
                      class="btn-icon"
                      title="Ver contrato"
                      (click)="
                        accion.emit({ id: grupo[0].Id, accion: 'contrato' })
                      "
                    >
                      <i class="fas fa-file-signature"></i>
                    </button>
                  }
                  @if (
                    !admin &&
                    (columna.estado === 'pendiente' ||
                      columna.estado === 'aprobado')
                  ) {
                    <button
                      class="btn btn-secondary"
                      (click)="
                        accion.emit({ id: grupo[0].Id, accion: 'cancelar' })
                      "
                    >
                      Cancelar
                    </button>
                  }
                  @if (admin) {
                    @if (columna.estado === 'pendiente') {
                      <button
                        class="btn-icon"
                        title="Aprobar préstamo"
                        (click)="
                          accion.emit({ id: grupo[0].Id, accion: 'aprobar' })
                        "
                      >
                        <i class="fas fa-check"></i>
                      </button>
                      <button
                        class="btn-icon"
                        title="Rechazar préstamo"
                        (click)="
                          accion.emit({ id: grupo[0].Id, accion: 'rechazar' })
                        "
                      >
                        <i class="fas fa-times"></i>
                      </button>
                    }
                    @if (columna.estado === 'aprobado') {
                      <button
                        class="btn btn-primary"
                        (click)="
                          accion.emit({ id: grupo[0].Id, accion: 'entregar' })
                        "
                      >
                        Entregar
                      </button>
                    }
                    @if (
                      columna.estado === 'activo' ||
                      columna.estado === 'atrasado'
                    ) {
                      <button
                        class="btn btn-primary"
                        (click)="
                          accion.emit({ id: grupo[0].Id, accion: 'devolver' })
                        "
                      >
                        Devolver
                      </button>
                    }
                    <button
                      class="btn-icon"
                      title="Editar observaciones"
                      (click)="
                        accion.emit({ id: grupo[0].Id, accion: 'observacion' })
                      "
                    >
                      <i class="fas fa-pencil-alt"></i>
                    </button>
                  }
                </div>
              </article>
            } @empty {
              <p class="loan-board__empty">Sin préstamos</p>
            }
          </div>
        </section>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-width: 0;
      }
      .loan-board {
        display: grid;
        grid-template-columns: repeat(5, minmax(240px, 1fr));
        gap: 16px;
        overflow-x: auto;
        padding: 4px 0 16px;
        align-items: start;
      }
      .loan-board__column {
        min-width: 0;
        border-top: 3px solid var(--color-border, #e2e8f0);
      }
      .loan-board__column:first-child {
        border-color: var(--color-primary, #ffd700);
      }
      h2 {
        font-size: 16px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin: 14px 0;
      }
      h2 span {
        font-weight: 500;
        color: var(--color-text-secondary, #475569);
      }
      .loan-board__items {
        display: grid;
        gap: 12px;
        max-height: 65dvh;
        overflow: auto;
      }
      .loan-board__card {
        padding: 16px;
        border: 1px solid var(--color-border, #e2e8f0);
        border-radius: 8px;
        background: white;
        min-width: 0;
      }
      .loan-board__meta {
        display: flex;
        justify-content: space-between;
        gap: 8px;
        color: var(--color-text-secondary, #475569);
        font-size: 13px;
      }
      .loan-board__title {
        border: 0;
        background: none;
        text-align: left;
        padding: 10px 0 4px;
        font: inherit;
        font-weight: 700;
        color: inherit;
        cursor: pointer;
        overflow-wrap: anywhere;
        min-height: 44px;
      }
      p {
        font-size: 14px;
        line-height: 1.6;
        margin: 6px 0;
        color: var(--color-text-secondary, #475569);
        overflow-wrap: anywhere;
      }
      .loan-board__actions {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin-top: 12px;
        align-items: center;
      }
      .loan-board__actions button {
        min-height: 44px;
      }
      .loan-board__empty {
        padding: 12px 0;
      }
      @media (max-width: 600px) {
        .loan-board {
          grid-template-columns: repeat(5, minmax(270px, 85vw));
        }
        .loan-board__items {
          max-height: 70dvh;
        }
      }
    `,
  ],
})
export class TableroPrestamosComponent implements OnChanges {
  @Input() prestamos: PrestamoDto[] = [];
  @Input() admin = false;
  @Output() detalle = new EventEmitter<PrestamoDto[]>();
  @Output() accion = new EventEmitter<AccionTablero>();
  columnas = ['Pendiente', 'Aprobado', 'Activo', 'Atrasado', 'Cerrados'].map(
    (nombre) => ({
      nombre,
      estado: nombre.toLowerCase(),
      filas: [] as PrestamoDto[][],
    }),
  );
  ngOnChanges() {
    const grupos = new Map<number, PrestamoDto[]>();
    for (const p of this.prestamos) {
      const grupo = grupos.get(p.Id) ?? [];
      grupo.push(p);
      grupos.set(p.Id, grupo);
    }
    const filas = [...grupos.values()].sort((a, b) =>
      compararPrestamos(a[0], b[0]),
    );
    this.columnas = this.columnas.map((c) => ({
      ...c,
      filas: filas.filter((g) =>
        c.estado === 'cerrados'
          ? ['finalizado', 'rechazado', 'cancelado'].includes(
              g[0].EstadoPrestamo?.toLowerCase() ?? '',
            )
          : g[0].EstadoPrestamo?.toLowerCase() === c.estado,
      ),
    }));
  }
}
