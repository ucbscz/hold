import { INVENTORY_LABELS } from '@brand/brand';
import { Component, Input } from '@angular/core';
import { estadoEquipoCssClass, estadoEquipoLabel } from '@entities/equipment';
import { Observable } from 'rxjs';
import { AdminInlineApiService } from '../api/admin-inline-api.service';
import { EquipoInlineItem } from '../model';
import { InlineListComponent } from './inline-list';
import { InlineSearchComponent } from './inline-search.component';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

@Component({
  selector: 'app-equipos-inline',
  standalone: true,
  imports: [InlineSearchComponent],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando...</p>
      } @else if (items.length === 0) {
        <p class="empty-inline">
          Sin equipos en este {{ porGavetero ? 'gavetero' : 'grupo' }}
        </p>
      } @else {
        <div class="audit-filters">
          <app-inline-search
            [(texto)]="filtroTexto"
            [placeholder]="
              porGavetero
                ? 'Buscar por código o serial...'
                : 'Buscar por código, serial o ubicación...'
            "
          ></app-inline-search>
        </div>
        @if (itemsFiltrados.length === 0) {
          <p class="empty-inline">Ningún equipo coincide con la búsqueda</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>{{ etiquetas.internalCode }}</th>
                <th>Estado</th>
                <th>Serial</th>
                @if (!porGavetero) {
                  <th>Ubicación</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (e of itemsFiltrados; track e.Id || e.CodigoImt || $index) {
                <tr>
                  <td>
                    <strong>IMT-{{ e.CodigoImt }}</strong>
                  </td>
                  <td>
                    <span
                      [class]="
                        'badge badge-estado-' +
                        estadoEquipoCssClass(e.EstadoEquipo)
                      "
                      >{{ estadoEquipoLabel(e.EstadoEquipo) }}</span
                    >
                  </td>
                  <td>{{ e.NumeroSerial || '—' }}</td>
                  @if (!porGavetero) {
                    <td>{{ e.Ubicacion || '—' }}</td>
                  }
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
export class EquiposInlineComponent extends InlineListComponent<EquipoInlineItem> {
  protected readonly etiquetas = INVENTORY_LABELS;
  @Input() grupoEquipoId?: number;
  @Input() gaveteroId?: number;
  protected readonly estadoEquipoLabel = estadoEquipoLabel;
  protected readonly estadoEquipoCssClass = estadoEquipoCssClass;

  constructor(private readonly api: AdminInlineApiService) {
    super();
  }

  get porGavetero(): boolean {
    return this.gaveteroId !== undefined;
  }

  protected cargar(): Observable<EquipoInlineItem[]> {
    return this.porGavetero
      ? this.api.obtenerEquiposPorGavetero(this.gaveteroId!)
      : this.api.obtenerEquiposPorGrupo(this.grupoEquipoId!);
  }

  protected camposBusqueda(equipo: EquipoInlineItem): unknown[] {
    return [equipo.CodigoImt, equipo.NumeroSerial, equipo.Ubicacion];
  }
}
