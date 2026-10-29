import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HistorialEquipoDto } from '@entities/admin';
import { estadoEquipoCssClass, estadoEquipoLabel } from '@entities/equipment';
import { ESTADO_PRESTAMO_FILTRO_OPCIONES } from '@entities/loan';
import { FlatpickrDirective } from '@shared/lib/directives';
import { CustomSelectComponent } from '@shared/ui';
import flatpickr from 'flatpickr';
import { Observable } from 'rxjs';
import { AdminInlineApiService } from '../api/admin-inline-api.service';
import { InlineListComponent } from './inline-list';
import { InlineSearchComponent } from './inline-search.component';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

@Component({
  selector: 'app-historial-equipo-inline',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    FlatpickrDirective,
    CustomSelectComponent,
    InlineSearchComponent,
  ],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando historial...</p>
      } @else {
        <div class="audit-filters">
          <app-inline-search
            [(texto)]="filtroTexto"
            placeholder="Buscar por # préstamo o usuario..."
          ></app-inline-search>
          <input
            type="text"
            appFlatpickr
            [fpOptions]="{ maxDate: 'today' }"
            class="date-input"
            placeholder="Fecha desde"
            readonly
            (fpChange)="onFechaDesde($event)"
            (fpReady)="fpDesde = $event"
          />
          <input
            type="text"
            appFlatpickr
            [fpOptions]="{ maxDate: 'today' }"
            class="date-input"
            placeholder="Fecha hasta"
            readonly
            (fpChange)="onFechaHasta($event)"
            (fpReady)="fpHasta = $event"
          />
          <app-custom-select
            class="inline-state-select"
            [(ngModel)]="filtroEstado"
            [ngModelOptions]="{ standalone: true }"
            [opciones]="estadoOpciones"
            icon="fas fa-filter"
            placeholder="Todos los estados"
          ></app-custom-select>
          <div class="audit-filters-actions">
            <button class="btn btn-ghost btn-sm" (click)="limpiarFiltros()">
              <i class="fas fa-times"></i> Limpiar
            </button>
          </div>
        </div>
        @if (items.length === 0) {
          <p class="empty-inline">Sin préstamos registrados para este equipo</p>
        } @else if (itemsFiltrados.length === 0) {
          <p class="empty-inline">Ningún préstamo coincide con los filtros</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Usuario</th>
                <th>Carnet</th>
                <th>Fecha Préstamo</th>
                <th>Fecha Devolución</th>
                <th>Estado</th>
                <th>Estado Equipo</th>
                <th>Observación</th>
              </tr>
            </thead>
            <tbody>
              @for (h of itemsFiltrados; track h.IdPrestamo + ':' + $index) {
                <tr>
                  <td>#{{ h.IdPrestamo }}</td>
                  <td>{{ h.NombreUsuario }}</td>
                  <td>{{ h.Carnet }}</td>
                  <td>
                    {{
                      h.FechaPrestamo
                        | date: 'dd/MM/yyyy HH:mm' : 'America/La_Paz'
                    }}
                  </td>
                  <td>
                    {{
                      h.FechaDevolucion || h.FechaDevolucionEsperada
                        | date: 'dd/MM/yyyy HH:mm' : 'America/La_Paz'
                    }}
                  </td>
                  <td>
                    <span
                      [class]="
                        'badge badge-' + (h.EstadoPrestamo || 'cancelado')
                      "
                      >{{ h.EstadoPrestamo }}</span
                    >
                  </td>
                  <td>
                    <span
                      [class]="
                        'badge badge-estado-' +
                        estadoEquipoCssClass(h.EstadoEquipo)
                      "
                      >{{ estadoEquipoLabel(h.EstadoEquipo) }}</span
                    >
                  </td>
                  @if (h.Observacion) {
                    <td
                      class="obs-cell"
                      (click)="observacionClick.emit(h.Observacion)"
                      title="Ver observación completa"
                    >
                      {{ h.Observacion }}
                    </td>
                  } @else {
                    <td>—</td>
                  }
                </tr>
              }
            </tbody>
          </table>
        }
      }
    </div>
  `,
  styles: [
    INLINE_SEARCH_STYLES,
    `
      .obs-cell {
        max-width: 220px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        cursor: pointer;
        color: var(--interactive-text);
      }
      .obs-cell:hover {
        text-decoration: underline;
      }

      .date-input {
        padding: 0.5rem 1.25rem;
        border: 1px solid var(--border);
        border-radius: var(--radius-full);
        font-size: 0.875rem;
        font-family: var(--font);
        color: var(--ink);
        background: rgba(255, 255, 255, 0.9);
        height: 42px;
        box-sizing: border-box;
        transition:
          border-color var(--t-fast),
          box-shadow var(--t-fast);
      }
      .date-input:focus {
        outline: none;
        border-color: var(--interactive-text);
        box-shadow: 0 0 0 3px
          color-mix(in srgb, var(--brand-secondary) 12%, transparent);
      }
      .date-input::placeholder {
        color: #757575;
      }
      .audit-filters-actions {
        display: flex;
        align-items: center;
        gap: 0.625rem;
      }
      .inline-state-select {
        width: 11rem;
        min-width: 11rem;
      }

      @media (max-width: 768px) {
        .date-input {
          width: 100%;
          max-width: none;
          min-width: 0;
          flex: 0 0 auto;
          height: 40px;
          padding: 0.4rem 1.1rem;
          font-size: 16px;
        }
        .audit-filters-actions {
          width: 100%;
        }
        .inline-state-select {
          width: 100%;
          min-width: 0;
        }
        .audit-filters-actions .btn {
          flex: 1;
          min-width: 0;
        }
      }
    `,
  ],
})
export class HistorialEquipoInlineComponent extends InlineListComponent<HistorialEquipoDto> {
  @Input() equipoId!: number;
  @Output() observacionClick = new EventEmitter<string>();
  filtroEstado = '';
  fechaDesde = '';
  fechaHasta = '';
  fpDesde?: flatpickr.Instance;
  fpHasta?: flatpickr.Instance;
  protected readonly estadoOpciones = ESTADO_PRESTAMO_FILTRO_OPCIONES;
  protected readonly estadoEquipoLabel = estadoEquipoLabel;
  protected readonly estadoEquipoCssClass = estadoEquipoCssClass;

  constructor(private readonly api: AdminInlineApiService) {
    super();
  }

  override get itemsFiltrados(): HistorialEquipoDto[] {
    const desde = this.fechaDesde ? new Date(this.fechaDesde) : null;
    const hasta = this.fechaHasta
      ? new Date(this.fechaHasta + 'T23:59:59')
      : null;
    return super.itemsFiltrados.filter(
      (h) =>
        (!this.filtroEstado || h.EstadoPrestamo === this.filtroEstado) &&
        this.dentroDeRango(h.FechaPrestamo, desde, hasta),
    );
  }

  onFechaDesde(dates: Date[]) {
    this.fechaDesde = this.fechaIso(dates);
  }

  onFechaHasta(dates: Date[]) {
    this.fechaHasta = this.fechaIso(dates);
  }

  limpiarFiltros() {
    this.filtroTexto = '';
    this.filtroEstado = '';
    this.fechaDesde = '';
    this.fechaHasta = '';
    this.fpDesde?.clear();
    this.fpHasta?.clear();
  }

  protected cargar(): Observable<HistorialEquipoDto[]> {
    return this.api.obtenerHistorialEquipo(this.equipoId);
  }

  protected camposBusqueda(historial: HistorialEquipoDto): unknown[] {
    return [historial.IdPrestamo, historial.NombreUsuario];
  }

  private dentroDeRango(
    fecha: Date | undefined,
    desde: Date | null,
    hasta: Date | null,
  ): boolean {
    if (!desde && !hasta) return true;
    if (!fecha) return false;
    return (!desde || fecha >= desde) && (!hasta || fecha <= hasta);
  }

  private fechaIso(dates: Date[]): string {
    return dates[0] ? dates[0].toISOString().split('T')[0] : '';
  }
}
