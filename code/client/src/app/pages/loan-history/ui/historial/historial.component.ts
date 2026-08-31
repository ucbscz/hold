import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { PrestamoDto } from '@entities/admin';
import {
  PrestamosAPIService,
  TableroPrestamosComponent,
  VistaPrestamosComponent,
  VercontratoComponent,
} from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { Subscription, finalize } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { FlatpickrDirective } from '@shared/lib/directives';
import { Aviso, OpcionSelect } from '@shared/ui';
import { extractErrorMessage } from '@shared/lib/error';
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
    TableroPrestamosComponent,
    VistaPrestamosComponent,
    VercontratoComponent,
    Aviso,
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
  readonly api = inject(PrestamosAPIService);
  readonly usuario = inject(UsuarioService);
  modo: 'tablero' | 'lista' = 'tablero';
  prestamos: PrestamoDto[] = [];
  detalle: PrestamoDto[] | null = null;
  contrato = signal(false);
  contratoId = 0;
  avisoCancelar = signal(false);
  cancelando = false;
  private cancelarId = 0;
  mensajeError = '';
  cargando = false;
  private solicitud?: Subscription;
  item = 'Pendiente';
  get prestamosFiltrados(): PrestamoDto[] {
    const texto = this.filtroTexto.trim().toLocaleLowerCase('es');
    return this.prestamos.filter((p) => {
      const fecha = new Date(p.FechaSolicitud ?? 0);
      const dia = new Intl.DateTimeFormat('en-CA', {
        timeZone: 'America/La_Paz',
      }).format(fecha);
      return (
        (!texto ||
          [p.Id, p.NombreGrupoEquipo]
            .join(' ')
            .toLocaleLowerCase('es')
            .includes(texto)) &&
        (!this.fechaDesde || dia >= this.fechaDesde) &&
        (!this.fechaHasta || dia <= this.fechaHasta)
      );
    });
  }
  abrirContrato(evento: { id: number; accion: string }): void {
    if (evento.accion === 'cancelar') {
      this.cancelarId = evento.id;
      this.avisoCancelar.set(true);
      return;
    }
    if (evento.accion !== 'contrato') return;
    this.contratoId = this.prestamos.some(
      (p) => p.Id === evento.id && p.IdContrato,
    )
      ? evento.id
      : 0;
    this.contrato.set(this.contratoId > 0);
  }
  cancelarPrestamo(): void {
    if (this.cancelando || !this.cancelarId) return;
    this.cancelando = true;
    this.api
      .cambiarEstadoPrestamo(this.cancelarId, 'cancelado')
      .pipe(finalize(() => (this.cancelando = false)))
      .subscribe({
        next: () => {
          this.avisoCancelar.set(false);
          this.cargarTablero();
        },
        error: (error) => {
          this.avisoCancelar.set(false);
          this.mensajeError = extractErrorMessage(error);
        },
      });
  }
  private cargarTablero(): void {
    if (this.cargando || this.modo !== 'tablero') return;
    const carnet = this.usuario.obtenerUsuario().carnet;
    if (!carnet) return;
    this.cargando = true;
    this.solicitud = this.api
      .obtenerPrestamosPorUsuario(carnet, '')
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: (prestamos) => {
          this.prestamos = prestamos;
          this.mensajeError = '';
        },
        error: () => {
          this.mensajeError =
            'No se pudieron cargar tus préstamos. Intenta nuevamente.';
        },
      });
  }
  verTablero(): void {
    this.modo = 'tablero';
    this.cargarTablero();
  }
  filtroTexto = '';
  fechaDesde = '';
  fechaHasta = '';
  refreshTrigger = 0;
  private pollInterval: ReturnType<typeof setInterval> | null = null;

  ngOnInit() {
    this.cargarTablero();
    this.pollInterval = setInterval(() => this.recargar(), 30000);
  }

  ngOnDestroy() {
    this.solicitud?.unsubscribe();
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  seleccionarEstado(valor: unknown): void {
    this.modo = 'lista';
    this.item = String(valor ?? 'Pendiente');
  }

  private recargar(): void {
    if (typeof document !== 'undefined' && document.hidden) return;

    this.refreshTrigger++;
    this.cargarTablero();
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
