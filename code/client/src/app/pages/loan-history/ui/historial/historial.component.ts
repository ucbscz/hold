import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FlatpickrDirective } from '@shared/lib/directives';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import flatpickr from 'flatpickr';
import { ActivoComponent } from './activo/activo.component';
import { AprobadoComponent } from './aprobado/aprobado.component';
import { AtrasadoComponent } from './atrasado/atrasado.component';
import { CanceladoComponent } from './cancelado/cancelado.component';
import { FinalizadoComponent } from './finalizado/finalizado.component';
import { PendienteComponent } from './pendiente/pendiente.component';
import { RechazadoComponent } from './rechazado/rechazado.component';
@Component({
  selector: 'app-historial',
  imports: [
    ActivoComponent,
    AprobadoComponent,
    AtrasadoComponent,
    CanceladoComponent,
    FinalizadoComponent,
    PendienteComponent,
    RechazadoComponent,
    FormsModule,
    FlatpickrDirective,
    CustomSelectComponent,
  ],
  templateUrl: './historial.component.html',
  styleUrl: './historial.component.css',
})
export class HistorialComponent implements OnInit, OnDestroy {
  readonly estados: OpcionSelect[] = [
    { value: 'Activo', label: 'Activos' },
    { value: 'Aprobado', label: 'Aprobados' },
    { value: 'Pendiente', label: 'Pendientes' },
    { value: 'Rechazado', label: 'Rechazados' },
    { value: 'Finalizado', label: 'Finalizados' },
    { value: 'Cancelado', label: 'Cancelados' },
    { value: 'Atrasado', label: 'Atrasados' },
  ];
  item = 'Activo';
  filtroTexto = '';
  fechaDesde = '';
  fechaHasta = '';
  refreshTrigger = 0;
  private pollInterval: ReturnType<typeof setInterval> | null = null;

  ngOnInit() {
    this.pollInterval = setInterval(() => this.recargar(), 30000);
  }

  ngOnDestroy() {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  seleccionarEstado(valor: unknown): void {
    this.item = String(valor ?? 'Activo');
  }

  private recargar(): void {
    if (typeof document !== 'undefined' && document.hidden) return;

    this.refreshTrigger++;
  }

  onFechaDesde(dates: Date[]) {
    this.fechaDesde = dates[0] ? dates[0].toISOString().split('T')[0] : '';
  }

  onFechaHasta(dates: Date[]) {
    this.fechaHasta = dates[0] ? dates[0].toISOString().split('T')[0] : '';
  }

  limpiarFiltros() {
    this.filtroTexto = '';
    this.fechaDesde = '';
    this.fechaHasta = '';
    this.fpDesde?.clear();
    this.fpHasta?.clear();
  }

  limpiarTextoBusqueda(): void {
    this.filtroTexto = '';
  }

  fpDesde?: flatpickr.Instance;
  fpHasta?: flatpickr.Instance;
}
