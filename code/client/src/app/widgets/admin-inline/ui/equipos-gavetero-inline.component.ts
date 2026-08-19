import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { EquipoInlineItem } from '../model';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

@Component({
  selector: 'app-equipos-gavetero-inline',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando...</p>
      } @else if (items.length === 0) {
        <p class="empty-inline">Sin equipos en este gavetero</p>
      } @else {
        <div class="audit-filters">
          <div class="audit-search-wrap">
            <i class="fas fa-search search-icon"></i>
            <input
              type="text"
              [(ngModel)]="filtroTexto"
              placeholder="Buscar por código o serial..."
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
          <p class="empty-inline">Ningún equipo coincide con la búsqueda</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>Código IMT</th>
                <th>Estado</th>
                <th>Serial</th>
              </tr>
            </thead>
            <tbody>
              @for (e of itemsFiltrados; track e.Id || e.CodigoImt || $index) {
                <tr>
                  <td>
                    <strong>IMT-{{ e.CodigoImt }}</strong>
                  </td>
                  <td>{{ e.EstadoEquipo }}</td>
                  <td>{{ e.NumeroSerial || '—' }}</td>
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
export class EquiposGaveteroInlineComponent implements OnInit {
  @Input() gaveteroId!: number;
  items: EquipoInlineItem[] = [];
  cargando = true;
  filtroTexto = '';

  constructor(private readonly http: HttpClient) {}

  get itemsFiltrados(): EquipoInlineItem[] {
    const texto = this.filtroTexto.trim().toLowerCase();
    if (!texto) return this.items;
    return this.items.filter(
      (e) =>
        e.CodigoImt?.toString().toLowerCase().includes(texto) ||
        (e.NumeroSerial ?? '').toLowerCase().includes(texto),
    );
  }

  ngOnInit() {
    this.http
      .get<ApiResponse<EquipoInlineItem[]>>(
        `${environment.apiUrl}/api/equipos/por-gavetero/${this.gaveteroId}`,
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
