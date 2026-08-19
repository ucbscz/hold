import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { PrestamoInlineItem } from '../model';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

@Component({
  selector: 'app-prestamos-inline',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando...</p>
      } @else if (items.length === 0) {
        <p class="empty-inline">Sin préstamos registrados</p>
      } @else {
        <div class="audit-filters">
          <div class="audit-search-wrap">
            <i class="fas fa-search search-icon"></i>
            <input
              type="text"
              [(ngModel)]="filtroTexto"
              placeholder="Buscar por # préstamo o equipo..."
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
        </div>
        @if (itemsFiltrados.length === 0) {
          <p class="empty-inline">Ningún préstamo coincide con la búsqueda</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Equipos</th>
                <th>Estado</th>
                <th>Solicitud</th>
                <th>Devolución Esperada</th>
              </tr>
            </thead>
            <tbody>
              @for (
                p of itemsFiltrados;
                track p.Id || p.NombreGrupoEquipo || $index
              ) {
                <tr>
                  <td>#{{ p.Id }}</td>
                  <td>{{ p.NombreGrupoEquipo || '—' }}</td>
                  <td>
                    <span
                      [class]="
                        'badge badge-' + (p.EstadoPrestamo || 'cancelado')
                      "
                      >{{ p.EstadoPrestamo }}</span
                    >
                  </td>
                  <td>
                    {{
                      p.FechaSolicitud
                        | date: 'dd/MM/yyyy HH:mm' : 'America/La_Paz'
                    }}
                  </td>
                  <td>
                    {{
                      p.FechaDevolucionEsperada
                        | date: 'dd/MM/yyyy HH:mm' : 'America/La_Paz'
                    }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      }
    </div>
  `,
  styles: [INLINE_SEARCH_STYLES],
})
export class PrestamosInlineComponent implements OnInit {
  @Input() carnet!: string;
  items: PrestamoInlineItem[] = [];
  cargando = true;
  filtroTexto = '';

  constructor(private readonly http: HttpClient) {}

  get itemsFiltrados(): PrestamoInlineItem[] {
    const texto = this.filtroTexto.trim().toLowerCase();
    if (!texto) return this.items;
    return this.items.filter(
      (p) =>
        p.Id?.toString().includes(texto) ||
        (p.NombreGrupoEquipo ?? '').toLowerCase().includes(texto),
    );
  }

  ngOnInit() {
    this.http
      .get<ApiResponse<PrestamoInlineItem[]>>(
        `${environment.apiUrl}/api/prestamos/por-usuario/${this.carnet}`,
      )
      .subscribe({
        next: (res) => {
          this.items = extractApiValue(res, []);
          this.cargando = false;
        },
        error: () => {
          this.cargando = false;
        },
      });
  }
}
