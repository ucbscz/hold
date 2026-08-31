import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FlatpickrDirective } from '@shared/lib/directives';
import { OpcionSelect } from '@shared/ui';
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
  ],
  templateUrl: './historial.component.html',
  styleUrl: './historial.component.css',
})
export class HistorialComponent implements OnInit, OnDestroy {
  readonly estados: OpcionSelect[] = [
    { value: 'Pendiente', label: 'Pendientes' },
    { value: 'Activo', label: 'Activos' },
    { value: 'Aprobado', label: 'Aprobados' },
    { value: 'Rechazado', label: 'Rechazados' },
    { value: 'Finalizado', label: 'Finalizados' },
    { value: 'Cancelado', label: 'Cancelados' },
    { value: 'Atrasado', label: 'Atrasados' },
  ];
  item = 'Pendiente';
  filtroTexto = '';
  fechaDesde = '';
  fechaHasta = '';
  refreshTrigger = 0;
  fpDesde?: flatpickr.Instance;
  fpHasta?: flatpickr.Instance;
  private pollInterval?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.pollInterval = setInterval(() => {
      if (!document.hidden) this.refreshTrigger++;
    }, 30000);
  }

  ngOnDestroy(): void {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  seleccionarEstado(valor: unknown): void {
    this.item = String(valor ?? 'Pendiente');
  }

  onFechaDesde(dates: Date[]): void {
    this.fechaDesde = dates[0] ? flatpickr.formatDate(dates[0], 'Y-m-d') : '';
  }

  onFechaHasta(dates: Date[]): void {
    this.fechaHasta = dates[0] ? flatpickr.formatDate(dates[0], 'Y-m-d') : '';
  }

  limpiarFiltros(): void {
    this.filtroTexto = '';
    this.fechaDesde = '';
    this.fechaHasta = '';
    this.fpDesde?.clear();
    this.fpHasta?.clear();
  }

  limpiarTextoBusqueda(): void {
    this.filtroTexto = '';
  }
}
