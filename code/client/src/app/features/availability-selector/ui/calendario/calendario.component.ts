import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Disponibilidad, DisponibilidadService } from '@entities/availability';
import { Carrito } from '@entities/cart';
import { extractErrorMessage } from '@shared/lib/error';
import { CustomSelectComponent, MostrarerrorComponent } from '@shared/ui';

const MINIMUM_DURATION_MINUTES = 30;
const MILLISECONDS_PER_MINUTE = 60 * 1000;
const MAXIMUM_END_TIME_MINUTES = 20 * 60;
type CampoFecha = 'inicio' | 'fin';

interface HoraOpcion {
  label: string;
  value: string;
}

@Component({
  selector: 'app-calendario',
  imports: [
    CommonModule,
    FormsModule,
    CustomSelectComponent,
    MostrarerrorComponent,
  ],
  templateUrl: './calendario.component.html',
  styleUrls: ['./calendario.component.css'],
})
export class CalendarioComponent {
  @Input() set entradaCarrito(value: Carrito) {
    this.carrito = value;
    this.ajustarFinAlLimite();
    this.consultarDisponibilidad();
  }
  @Input() fechaInicioSeleccionada: WritableSignal<Date | null> = signal(null);
  @Input() fechaFinSeleccionada: WritableSignal<Date | null> = signal(null);
  @Input() soloAvisoFechasOcupadas = false;
  @Input() mostrarEncabezado = true;
  @Output() avisarDisponibilidad = new EventEmitter<Date>();

  carrito: Carrito = {};
  disponibilidad: Disponibilidad[] = [];
  cargando = false;
  consultado = false;
  campoActivo: CampoFecha = 'inicio';
  mesVisible = this.inicioDelMes(new Date());
  error: WritableSignal<boolean> = signal(false);
  mensajeerror = 'No se pudo consultar la disponibilidad.';
  readonly horas: HoraOpcion[] = this.crearHoras();
  private readonly minimoInicio = this.siguienteBloque();

  constructor(private readonly apiDisponibilidad: DisponibilidadService) {}

  ngOnInit(): void {
    this.inicializarRango();
    this.mesVisible = this.inicioDelMes(
      this.fechaInicioSeleccionada() ?? this.minimoInicio,
    );
    this.consultarDisponibilidad();
  }

  get diasDelMes(): Array<Date | null> {
    const primerDia = this.inicioDelMes(this.mesVisible);
    const primerIndice = (primerDia.getDay() + 6) % 7;
    const dias: Array<Date | null> = Array.from(
      { length: primerIndice },
      () => null,
    );
    const ultimoDia = new Date(
      primerDia.getFullYear(),
      primerDia.getMonth() + 1,
      0,
    ).getDate();

    for (let dia = 1; dia <= ultimoDia; dia++) {
      dias.push(new Date(primerDia.getFullYear(), primerDia.getMonth(), dia));
    }

    while (dias.length % 7 !== 0) dias.push(null);
    return dias;
  }

  get etiquetaMes(): string {
    return new Intl.DateTimeFormat('es-BO', {
      month: 'long',
      year: 'numeric',
    }).format(this.mesVisible);
  }

  get etiquetaCampoActivo(): string {
    return this.campoActivo === 'inicio' ? 'inicio' : 'devolución';
  }

  get rangoValido(): boolean {
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();

    return !!inicio && !!fin && this.esRangoValido(inicio, fin);
  }

  get maximoDiasPrestamo(): number | null {
    const limits = Object.values(this.carrito)
      .map((item) => item.tiempoMaximoPrestamoDias)
      .filter((days) => Number.isFinite(days) && days > 0);

    return limits.length > 0 ? Math.min(...limits) : null;
  }

  get mensajeRangoInvalido(): string {
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();
    const maximoDias = this.maximoDiasPrestamo;

    if (
      inicio &&
      fin &&
      maximoDias != null &&
      fin.getTime() - inicio.getTime() >
        maximoDias * 24 * 60 * MILLISECONDS_PER_MINUTE
    )
      return `El préstamo no puede superar ${maximoDias} día(s) para los equipos seleccionados.`;

    return 'Elige una devolución de al menos 30 minutos después del inicio.';
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

  get horasDisponibles(): HoraOpcion[] {
    return this.horas.filter(
      (hora) => !this.horaDeshabilitada(this.campoActivo, hora.value),
    );
  }

  seleccionarCampo(campo: CampoFecha): void {
    this.campoActivo = campo;
    const fecha = this.obtenerFecha(campo);
    if (fecha) this.mesVisible = this.inicioDelMes(fecha);
  }

  cambiarMes(desplazamiento: number): void {
    const siguienteMes = new Date(
      this.mesVisible.getFullYear(),
      this.mesVisible.getMonth() + desplazamiento,
      1,
    );

    if (
      this.comparaSoloFecha(
        siguienteMes,
        this.inicioDelMes(this.minimoInicio),
      ) < 0
    )
      return;

    this.mesVisible = siguienteMes;
  }

  puedeRetrocederMes(): boolean {
    return (
      this.comparaSoloFecha(
        this.mesVisible,
        this.inicioDelMes(this.minimoInicio),
      ) > 0
    );
  }

  seleccionarDia(dia: Date): void {
    if (this.esDiaDeshabilitado(dia)) return;

    const fechaActual =
      this.obtenerFecha(this.campoActivo) ?? this.minimoInicio;
    const fechaSeleccionada = this.combinarDiaYHora(dia, fechaActual);

    if (this.campoActivo === 'inicio') {
      const inicio = this.normalizarInicio(fechaSeleccionada);
      this.fechaInicioSeleccionada.set(inicio);
      this.ajustarFinAlInicio(inicio);
    } else {
      const inicio = this.fechaInicioSeleccionada() ?? this.minimoInicio;
      this.fechaFinSeleccionada.set(
        this.normalizarFin(
          fechaSeleccionada,
          this.sumarMinutos(inicio, MINIMUM_DURATION_MINUTES),
        ),
      );
    }

    this.consultarDisponibilidad();
  }

  cambiarHora(campo: CampoFecha, valor: string): void {
    const [horas, minutos] = valor.split(':').map(Number);
    const fechaActual = this.obtenerFecha(campo) ?? this.minimoInicio;
    const fechaConHora = new Date(fechaActual);
    fechaConHora.setHours(horas, minutos, 0, 0);

    if (campo === 'inicio') {
      const inicio = this.normalizarInicio(fechaConHora);
      this.fechaInicioSeleccionada.set(inicio);
      this.ajustarFinAlInicio(inicio);
    } else {
      const inicio = this.fechaInicioSeleccionada() ?? this.minimoInicio;
      this.fechaFinSeleccionada.set(
        this.normalizarFin(
          fechaConHora,
          this.sumarMinutos(inicio, MINIMUM_DURATION_MINUTES),
        ),
      );
    }

    this.consultarDisponibilidad();
  }

  esDiaDeshabilitado(dia: Date): boolean {
    const minimo =
      this.campoActivo === 'inicio'
        ? this.minimoInicio
        : (this.fechaInicioSeleccionada() ?? this.minimoInicio);
    if (this.comparaSoloFecha(dia, minimo) < 0) return true;

    const limiteMaximo = this.limiteMaximoPrestamo();
    return (
      this.campoActivo === 'fin' &&
      limiteMaximo != null &&
      this.comparaSoloFecha(dia, limiteMaximo) > 0
    );
  }

  esDiaSeleccionado(dia: Date, campo: CampoFecha): boolean {
    const fecha = this.obtenerFecha(campo);
    return !!fecha && this.comparaSoloFecha(dia, fecha) === 0;
  }

  esDiaEnRango(dia: Date): boolean {
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();
    if (!inicio || !fin) return false;

    return (
      this.comparaSoloFecha(dia, inicio) > 0 &&
      this.comparaSoloFecha(dia, fin) < 0
    );
  }

  esHoy(dia: Date): boolean {
    return this.comparaSoloFecha(dia, new Date()) === 0;
  }

  horaSeleccionada(campo: CampoFecha): string {
    const fecha = this.obtenerFecha(campo) ?? this.minimoInicio;
    return `${this.dosDigitos(fecha.getHours())}:${this.dosDigitos(fecha.getMinutes())}`;
  }

  horaDeshabilitada(campo: CampoFecha, hora: string): boolean {
    const [horas, minutos] = hora.split(':').map(Number);
    const minutosDelDia = horas * MINIMUM_DURATION_MINUTES * 2 + minutos;
    const limite =
      campo === 'inicio'
        ? MAXIMUM_END_TIME_MINUTES - MINIMUM_DURATION_MINUTES
        : MAXIMUM_END_TIME_MINUTES;
    if (minutosDelDia > limite) return true;

    const fecha = new Date(this.obtenerFecha(campo) ?? this.minimoInicio);
    fecha.setHours(horas, minutos, 0, 0);
    const minimo =
      campo === 'inicio'
        ? this.minimoInicio
        : this.sumarMinutos(
            this.fechaInicioSeleccionada() ?? this.minimoInicio,
            MINIMUM_DURATION_MINUTES,
          );
    if (fecha.getTime() < minimo.getTime()) return true;

    const limiteMaximo = this.limiteMaximoPrestamo();
    return (
      campo === 'fin' &&
      limiteMaximo != null &&
      fecha.getTime() > limiteMaximo.getTime()
    );
  }

  formatearFecha(fecha: Date | null): string {
    if (!fecha) return 'Sin seleccionar';
    return new Intl.DateTimeFormat('es-BO', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(fecha);
  }

  solicitarAviso(): void {
    const inicio = this.fechaInicioSeleccionada();
    if (inicio) this.avisarDisponibilidad.emit(inicio);
  }

  private inicializarRango(): void {
    if (this.fechaInicioSeleccionada() && this.fechaFinSeleccionada()) return;

    const inicio = this.minimoInicio;
    this.fechaInicioSeleccionada.set(inicio);
    this.fechaFinSeleccionada.set(
      this.sumarMinutos(inicio, MINIMUM_DURATION_MINUTES),
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

  private obtenerFecha(campo: CampoFecha): Date | null {
    return campo === 'inicio'
      ? this.fechaInicioSeleccionada()
      : this.fechaFinSeleccionada();
  }

  private normalizarInicio(fecha: Date): Date {
    const minimo = this.minimoInicio;
    const normalizada =
      fecha.getTime() < minimo.getTime() ? new Date(minimo) : new Date(fecha);
    const limite = this.limiteDelDia(
      normalizada,
      MAXIMUM_END_TIME_MINUTES - MINIMUM_DURATION_MINUTES,
    );

    return normalizada.getTime() > limite.getTime()
      ? this.inicioDelSiguienteDia(normalizada)
      : normalizada;
  }

  private normalizarFin(fecha: Date, minimo: Date): Date {
    let normalizada =
      fecha.getTime() < minimo.getTime() ? new Date(minimo) : new Date(fecha);
    const limite = this.limiteDelDia(normalizada, MAXIMUM_END_TIME_MINUTES);

    if (normalizada.getTime() > limite.getTime()) normalizada = limite;

    const limiteMaximo = this.limiteMaximoPrestamo();
    if (limiteMaximo && normalizada.getTime() > limiteMaximo.getTime())
      normalizada = limiteMaximo;

    return normalizada;
  }

  private crearHoras(): HoraOpcion[] {
    return Array.from(
      { length: MAXIMUM_END_TIME_MINUTES / MINIMUM_DURATION_MINUTES + 1 },
      (_, indice) => {
        const horas = Math.floor(indice / 2);
        const minutos = (indice % 2) * MINIMUM_DURATION_MINUTES;
        const value = `${this.dosDigitos(horas)}:${this.dosDigitos(minutos)}`;
        return { value, label: value };
      },
    );
  }

  private combinarDiaYHora(dia: Date, hora: Date): Date {
    const resultado = new Date(dia);
    resultado.setHours(hora.getHours(), hora.getMinutes(), 0, 0);
    return resultado;
  }

  private inicioDelMes(fecha: Date): Date {
    return new Date(fecha.getFullYear(), fecha.getMonth(), 1);
  }

  private compararFechas(a: Date, b: Date): number {
    return a.getTime() - b.getTime();
  }

  private comparaSoloFecha(a: Date, b: Date): number {
    return this.compararFechas(
      new Date(a.getFullYear(), a.getMonth(), a.getDate()),
      new Date(b.getFullYear(), b.getMonth(), b.getDate()),
    );
  }

  private esRangoValido(inicio: Date, fin: Date): boolean {
    const duration = fin.getTime() - inicio.getTime();
    const maximumDays = this.maximoDiasPrestamo;

    return (
      duration >= MINIMUM_DURATION_MINUTES * MILLISECONDS_PER_MINUTE &&
      (maximumDays == null ||
        duration <= maximumDays * 24 * 60 * MILLISECONDS_PER_MINUTE)
    );
  }

  private limiteMaximoPrestamo(): Date | null {
    const inicio = this.fechaInicioSeleccionada();
    const maximumDays = this.maximoDiasPrestamo;

    return inicio && maximumDays != null
      ? new Date(
          inicio.getTime() + maximumDays * 24 * 60 * MILLISECONDS_PER_MINUTE,
        )
      : null;
  }

  private ajustarFinAlInicio(inicio: Date): void {
    const minimo = this.sumarMinutos(inicio, MINIMUM_DURATION_MINUTES);
    const fin = this.fechaFinSeleccionada() ?? minimo;
    this.fechaFinSeleccionada.set(this.normalizarFin(fin, minimo));
  }

  private ajustarFinAlLimite(): void {
    const inicio = this.fechaInicioSeleccionada();
    if (inicio) this.ajustarFinAlInicio(inicio);
  }

  private sumarMinutos(fecha: Date, minutos: number): Date {
    return new Date(fecha.getTime() + minutos * MILLISECONDS_PER_MINUTE);
  }

  private limiteDelDia(fecha: Date, minutosDelDia: number): Date {
    const limite = new Date(fecha);
    limite.setHours(Math.floor(minutosDelDia / 60), minutosDelDia % 60, 0, 0);
    return limite;
  }

  private inicioDelSiguienteDia(fecha: Date): Date {
    const siguienteDia = new Date(fecha);
    siguienteDia.setDate(siguienteDia.getDate() + 1);
    siguienteDia.setHours(0, 0, 0, 0);
    return siguienteDia;
  }

  private dosDigitos(valor: number): string {
    return String(valor).padStart(2, '0');
  }

  private siguienteBloque(): Date {
    const fecha = new Date();
    fecha.setSeconds(0, 0);
    fecha.setMinutes(
      Math.ceil(fecha.getMinutes() / MINIMUM_DURATION_MINUTES) *
        MINIMUM_DURATION_MINUTES,
    );
    if (fecha <= new Date()) {
      fecha.setMinutes(fecha.getMinutes() + MINIMUM_DURATION_MINUTES);
    }
    if (
      fecha.getHours() * 60 + fecha.getMinutes() >
      MAXIMUM_END_TIME_MINUTES - MINIMUM_DURATION_MINUTES
    ) {
      return this.inicioDelSiguienteDia(fecha);
    }
    return fecha;
  }
}
