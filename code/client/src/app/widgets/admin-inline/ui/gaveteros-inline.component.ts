import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { GaveteroInlineItem } from '../model';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

@Component({
  selector: 'app-gaveteros-inline',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando...</p>
      } @else if (items.length === 0) {
        <p class="empty-inline">Sin gaveteros en este mueble</p>
      } @else {
        <div class="audit-filters">
          <div class="audit-search-wrap">
            <i class="fas fa-search search-icon"></i>
            <input
              type="text"
              [(ngModel)]="filtroTexto"
              placeholder="Buscar por nombre..."
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
          <p class="empty-inline">Ningún gavetero coincide con la búsqueda</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Gaveteros</th>
              </tr>
            </thead>
            <tbody>
              @for (g of itemsFiltrados; track g.Id || g.Nombre || $index) {
                <tr>
                  <td>{{ g.Nombre }}</td>
                  <td>{{ g.NumeroGaveteros ?? 0 }}</td>
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
export class GaveterosInlineComponent implements OnInit {
  @Input() muebleId!: number;
  items: GaveteroInlineItem[] = [];
  cargando = true;
  filtroTexto = '';

  constructor(private readonly http: HttpClient) {}

  get itemsFiltrados(): GaveteroInlineItem[] {
    const texto = this.filtroTexto.trim().toLowerCase();
    if (!texto) return this.items;
    return this.items.filter((g) =>
      String(g.Nombre ?? '')
        .toLowerCase()
        .includes(texto),
    );
  }

  ngOnInit() {
    this.http
      .get<ApiResponse<GaveteroInlineItem[]>>(
        `${environment.apiUrl}/api/gaveteros/por-mueble/${this.muebleId}`,
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
