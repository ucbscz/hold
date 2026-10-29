import { Component, Input } from '@angular/core';
import { Observable } from 'rxjs';
import { GaveteroService } from '../api/gavetero.service';
import { GaveteroInlineItem } from '../model/gavetero-inline-item';
import {
  INLINE_SEARCH_STYLES,
  InlineListComponent,
  InlineSearchComponent,
} from '@shared/ui';

@Component({
  selector: 'app-gaveteros-inline',
  standalone: true,
  imports: [InlineSearchComponent],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando...</p>
      } @else if (items.length === 0) {
        <p class="empty-inline">Sin gaveteros en este mueble</p>
      } @else {
        <div class="audit-filters">
          <app-inline-search
            [(texto)]="filtroTexto"
            placeholder="Buscar por nombre..."
          ></app-inline-search>
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
export class GaveterosInlineComponent extends InlineListComponent<GaveteroInlineItem> {
  @Input() muebleId!: number;

  constructor(private readonly api: GaveteroService) {
    super();
  }

  protected cargar(): Observable<GaveteroInlineItem[]> {
    return this.api.obtenerGaveterosPorMueble(this.muebleId);
  }

  protected camposBusqueda(gavetero: GaveteroInlineItem): unknown[] {
    return [gavetero.Nombre];
  }
}
