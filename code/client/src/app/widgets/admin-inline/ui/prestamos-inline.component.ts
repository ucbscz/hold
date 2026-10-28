import { DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminInlineApiService } from '../api/admin-inline-api.service';
import { PrestamoInlineItem } from '../model';
import { InlineListComponent } from './inline-list';
import { InlineSearchComponent } from './inline-search.component';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

@Component({
  selector: 'app-prestamos-inline',
  standalone: true,
  imports: [DatePipe, InlineSearchComponent],
  template: `
    <div class="inline-panel">
      @if (cargando) {
        <p class="empty-inline">Cargando...</p>
      } @else if (items.length === 0) {
        <p class="empty-inline">Sin préstamos registrados</p>
      } @else {
        <div class="audit-filters">
          <app-inline-search
            [(texto)]="filtroTexto"
            placeholder="Buscar por # préstamo o equipo..."
          ></app-inline-search>
        </div>
        @if (itemsFiltrados.length === 0) {
          <p class="empty-inline">Ningún préstamo coincide con la búsqueda</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Equipo</th>
                <th>Identificación</th>
                <th>Ubicación</th>
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
                    <div class="equipment-identifiers">
                      <span
                        ><strong>IMT</strong>
                        {{ p.CodigoImt || 'No asignado' }}</span
                      >
                      @if (p.CodigoUcb) {
                        <span><strong>UCB</strong> {{ p.CodigoUcb }}</span>
                      }
                      @if (p.NumeroSerial) {
                        <span><strong>Serie</strong> {{ p.NumeroSerial }}</span>
                      }
                    </div>
                  </td>
                  <td>
                    <div class="equipment-location">
                      <span>{{
                        p.UbicacionEquipo || 'Sin ambiente asignado'
                      }}</span>
                      @if (p.NombreMueble) {
                        <span>Mueble: {{ p.NombreMueble }}</span>
                      }
                      @if (p.NombreGavetero) {
                        <span>Gavetero: {{ p.NombreGavetero }}</span>
                      }
                      @if (p.UbicacionMueble) {
                        <span>Referencia: {{ p.UbicacionMueble }}</span>
                      }
                    </div>
                  </td>
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
export class PrestamosInlineComponent extends InlineListComponent<PrestamoInlineItem> {
  @Input() carnet!: string;

  constructor(private readonly api: AdminInlineApiService) {
    super();
  }

  protected cargar(): Observable<PrestamoInlineItem[]> {
    return this.api.obtenerPrestamosPorCarnet(this.carnet);
  }

  protected camposBusqueda(prestamo: PrestamoInlineItem): unknown[] {
    return [
      prestamo.Id,
      prestamo.NombreGrupoEquipo,
      prestamo.CodigoImt,
      prestamo.CodigoUcb,
      prestamo.NumeroSerial,
      prestamo.UbicacionEquipo,
      prestamo.NombreMueble,
      prestamo.NombreGavetero,
    ];
  }
}
