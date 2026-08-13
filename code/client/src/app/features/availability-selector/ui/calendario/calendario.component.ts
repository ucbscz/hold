import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { Disponibilidad, DisponibilidadService } from '@entities/availability';
import { Carrito } from '@entities/cart';
import { FlatpickrDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import { MostrarerrorComponent } from '@shared/ui';
import { Options } from 'flatpickr/dist/types/options';

const MINIMUM_DURATION_MINUTES = 30;

@Component({
  selector: 'app-calendario',
  imports: [CommonModule, FlatpickrDirective, MostrarerrorComponent],
  templateUrl: './calendario.component.html',
  styleUrls: ['./calendario.component.css'],
})
export class CalendarioComponent {
  @Input() set entradaCarrito(value: Carrito) {
    this.carrito = value;
    this.consultarDisponibilidad();
  }
  @Input() fechaInicioSeleccionada: WritableSignal<Date | null> = signal(null);
  @Input() fechaFinSeleccionada: WritableSignal<Date | null> = signal(null);
  @Input() soloAvisoFechasOcupadas = false;
  @Output() avisarDisponibilidad = new EventEmitter<Date>();

  carrito: Carrito = {};
  disponibilidad: Disponibilidad[] = [];
  cargando = false;
  consultado = false;
  error: WritableSignal<boolean> = signal(false);
  mensajeerror = 'No se pudo consultar la disponibilidad.';
  private readonly minimoInicio = this.siguienteBloque();

  constructor(private readonly apiDisponibilidad: DisponibilidadService) {}

  ngOnInit(): void {
    this.inicializarRango();
    this.consultarDisponibilidad();
  }

  get opcionesInicio(): Partial<Options> {
    return {
      enableTime: true,
      time_24hr: true,
      minuteIncrement: MINIMUM_DURATION_MINUTES,
      dateFormat: 'Y-m-d H:i',
      minDate: this.minimoInicio,
    };
  }

  get opcionesFin(): Partial<Options> {
    const inicio = this.fechaInicioSeleccionada();

    return {
      enableTime: true,
      time_24hr: true,
      minuteIncrement: MINIMUM_DURATION_MINUTES,
      dateFormat: 'Y-m-d H:i',
      minDate: inicio
        ? new Date(inicio.getTime() + MINIMUM_DURATION_MINUTES * 60 * 1000)
        : this.minimoInicio,
    };
  }

  onFechaInicio(dates: Date[]): void {
    const fechaInicio = dates[0] ?? null;
    this.fechaInicioSeleccionada.set(fechaInicio);

    const fechaFin = this.fechaFinSeleccionada();
    if (
      fechaInicio &&
      (!fechaFin ||
        fechaFin.getTime() - fechaInicio.getTime() <
          MINIMUM_DURATION_MINUTES * 60 * 1000)
    ) {
      this.fechaFinSeleccionada.set(
        new Date(fechaInicio.getTime() + MINIMUM_DURATION_MINUTES * 60 * 1000),
      );
    }

    this.consultarDisponibilidad();
  }

  onFechaFin(dates: Date[]): void {
    this.fechaFinSeleccionada.set(dates[0] ?? null);
    this.consultarDisponibilidad();
  }

  formatearFechaHora(fecha: Date | null): string {
    if (!fecha) return '';

    const pad = (value: number) => String(value).padStart(2, '0');
    return `${fecha.getFullYear()}-${pad(fecha.getMonth() + 1)}-${pad(fecha.getDate())} ${pad(fecha.getHours())}:${pad(fecha.getMinutes())}`;
  }

  get rangoValido(): boolean {
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();

    return (
      !!inicio &&
      !!fin &&
      fin.getTime() - inicio.getTime() >= MINIMUM_DURATION_MINUTES * 60 * 1000
    );
  }

  get hayDisponibilidad(): boolean {
    if (!this.consultado || !this.rangoValido) return false;

    return Object.entries(this.carrito).every(([id, item]) => {
      const disponibilidad = this.disponibilidad.find(
        (resultado) => resultado.IdGrupoEquipo === Number(id),
      );

      return (disponibilidad?.CantidadDisponible ?? 0) >= item.cantidad;
    });
  }

  get cantidadDisponible(): number {
    if (this.disponibilidad.length !== 1) return 0;

    return this.disponibilidad[0].CantidadDisponible;
  }

  solicitarAviso(): void {
    const inicio = this.fechaInicioSeleccionada();

    if (inicio) this.avisarDisponibilidad.emit(inicio);
  }

  private inicializarRango(): void {
    if (this.fechaInicioSeleccionada() && this.fechaFinSeleccionada()) return;

    const inicio = this.siguienteBloque();
    this.fechaInicioSeleccionada.set(inicio);
    this.fechaFinSeleccionada.set(
      new Date(inicio.getTime() + MINIMUM_DURATION_MINUTES * 60 * 1000),
    );
  }

  private consultarDisponibilidad(): void {
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();
    const ids = Object.keys(this.carrito).map(Number);

    if (!inicio || !fin || !this.rangoValido || ids.length === 0) {
      this.disponibilidad = [];
      this.consultado = false;
      return;
    }

    this.cargando = true;
    this.error.set(false);
    this.apiDisponibilidad.obtenerDisponibilidad(inicio, fin, ids).subscribe({
      next: (data) => {
        this.disponibilidad = data;
        this.consultado = true;
        this.cargando = false;
      },
      error: (error) => {
        this.mensajeerror = extractErrorMessage(
          error,
          'No se pudo consultar la disponibilidad.',
        );
        this.cargando = false;
        this.consultado = false;
        this.error.set(true);
      },
    });
  }

  private siguienteBloque(): Date {
    const fecha = new Date();
    fecha.setSeconds(0, 0);
    fecha.setMinutes(
      Math.ceil(fecha.getMinutes() / MINIMUM_DURATION_MINUTES) *
        MINIMUM_DURATION_MINUTES,
    );
    if (fecha <= new Date())
      fecha.setMinutes(fecha.getMinutes() + MINIMUM_DURATION_MINUTES);

    return fecha;
  }
}
