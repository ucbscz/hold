import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HistorialEquipoDto } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { FlatpickrDirective } from '@shared/lib/directives';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import flatpickr from 'flatpickr';
import { EquipoHistorialInlineItem } from '../model';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

const ESTADOS_PRESTAMO = [
  'pendiente',
  'aprobado',
  'activo',
  'atrasado',
  'finalizado',
  'rechazado',
  'cancelado',
];

@Component({
  selector: 'app-historial-equipo-inline',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    FlatpickrDirective,
    CustomSelectComponent,
  ],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando historial...</p>
      } @else {
        <div class="audit-filters">
          <div class="audit-search-wrap">
            <i class="fas fa-search search-icon"></i>
            <input
              type="text"
              [(ngModel)]="filtroTexto"
              placeholder="Buscar por # préstamo o usuario..."
              class="admin-search"
            />
            @if (filtroTexto) {
              <button
                class="clear-search"
                (click)="filtroTexto = ''"
                title="Limpiar búsqueda"
                type="button"
              >
                <i class="fas fa-times"></i>
              </button>
            }
          </div>
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
              @for (h of itemsFiltrados; track h.IdPrestamo) {
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
                      >{{ estadoEquipoBadgeLabel(h.EstadoEquipo) }}</span
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
      .badge-estado-operativo {
        background: var(--success-bg);
        color: var(--success);
      }
      .badge-estado-parcialmente-operativo {
        background: #fff7ed;
        color: #b45309;
      }
      .badge-estado-inoperativo {
        background: var(--error-bg);
        color: var(--error);
      }
      .badge-estado-none {
        background: var(--sidebar);
        color: var(--ink-muted);
      }
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
        box-shadow: 0 0 0 3px rgba(0, 69, 123, 0.12);
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
export class HistorialEquipoInlineComponent implements OnInit {
  @Input() equipoId!: number;
  @Output() observacionClick = new EventEmitter<string>();
  items: HistorialEquipoDto[] = [];
  cargando = true;

  filtroTexto = '';
  filtroEstado = '';
  fechaDesde = '';
  fechaHasta = '';
  estadosPrestamo = ESTADOS_PRESTAMO;
  readonly estadoOpciones: OpcionSelect[] = [
    { value: '', label: 'Todos los estados' },
    ...ESTADOS_PRESTAMO.map((estado) => ({
      value: estado,
      label: estado.charAt(0).toUpperCase() + estado.slice(1),
    })),
  ];
  fpDesde?: flatpickr.Instance;
  fpHasta?: flatpickr.Instance;

  constructor(private readonly http: HttpClient) {}

  get itemsFiltrados(): HistorialEquipoDto[] {
    const texto = this.filtroTexto.trim().toLowerCase();
    const desde = this.fechaDesde ? new Date(this.fechaDesde) : null;
    const hasta = this.fechaHasta
      ? new Date(this.fechaHasta + 'T23:59:59')
      : null;
    return this.items.filter((h) => {
      if (texto) {
        const matchId = h.IdPrestamo?.toString().includes(texto) ?? false;
        const matchNombre =
          h.NombreUsuario?.toLowerCase().includes(texto) ?? false;
        if (!matchId && !matchNombre) return false;
      }
      if (this.filtroEstado && h.EstadoPrestamo !== this.filtroEstado)
        return false;
      if (desde || hasta) {
        const fecha = h.FechaPrestamo ? new Date(h.FechaPrestamo) : null;
        if (!fecha) return false;
        if (desde && fecha < desde) return false;
        if (hasta && fecha > hasta) return false;
      }
      return true;
    });
  }

  estadoEquipoLabel(estado?: string): string {
    switch (estado) {
      case 'operativo':
        return 'Operativo';
      case 'parcialmente_operativo':
        return 'Parcialmente operativo';
      case 'inoperativo':
        return 'Inoperativo';
      case 'pendiente':
        return 'Pendiente';
      case 'aprobado':
        return 'Aprobado';
      case 'activo':
        return 'Activo';
      case 'atrasado':
        return 'Atrasado';
      case 'finalizado':
        return 'Finalizado';
      case 'rechazado':
        return 'Rechazado';
      case 'cancelado':
        return 'Cancelado';
      default:
        return '—';
    }
  }

  estadoEquipoBadgeLabel(estado?: string): string {
    return estado ? this.estadoEquipoLabel(estado) : '—';
  }

  estadoEquipoCssClass(estado?: string): string {
    return estado ? estado.replaceAll('_', '-') : 'none';
  }

  onFechaDesde(dates: Date[]) {
    this.fechaDesde = dates[0] ? dates[0].toISOString().split('T')[0] : '';
  }

  onFechaHasta(dates: Date[]) {
    this.fechaHasta = dates[0] ? dates[0].toISOString().split('T')[0] : '';
  }

  limpiarFiltros() {
    this.filtroTexto = '';
    this.filtroEstado = '';
    this.fechaDesde = '';
    this.fechaHasta = '';
    this.fpDesde?.clear();
    this.fpHasta?.clear();
  }

  ngOnInit() {
    this.http
      .get<ApiResponse<EquipoHistorialInlineItem[]>>(
        `${environment.apiUrl}/api/equipo/${this.equipoId}/historial`,
      )
      .subscribe({
        next: (res) => {
          const data = extractApiValue(res, []);
          this.items = data.map((item) => ({
            IdPrestamo: item.IdPrestamo,
            NombreUsuario: item.NombreUsuario ?? undefined,
            Carnet: item.Carnet ?? undefined,
            EstadoPrestamo: item.EstadoPrestamo ?? undefined,
            EstadoEquipo: item.EstadoEquipo ?? undefined,
            Observacion: item.Observacion ?? undefined,
            FechaPrestamo: item.FechaPrestamo
              ? new Date(item.FechaPrestamo)
              : undefined,
            FechaDevolucionEsperada: item.FechaDevolucionEsperada
              ? new Date(item.FechaDevolucionEsperada)
              : undefined,
            FechaDevolucion: item.FechaDevolucion
              ? new Date(item.FechaDevolucion)
              : undefined,
          }));
          this.cargando = false;
        },
        error: () => {
          this.cargando = false;
        },
      });
  }
}
