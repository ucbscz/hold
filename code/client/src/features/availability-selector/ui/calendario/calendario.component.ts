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
import {
  ConfiguracionService,
  horarioParaFecha,
} from '@entities/configuracion';
import {
  Disponibilidad,
  DisponibilidadDia,
  DisponibilidadService,
} from '@entities/availability';
import { Carrito } from '@entities/cart';
import { extractErrorMessage } from '@shared/lib/error';
import { CustomSelectComponent, MostrarerrorComponent } from '@shared/ui';
import {
  HoraOpcion,
  MILLISECONDS_PER_DAY,
  claveFecha,
  combinarDiaYHora,
  comparaSoloFecha,
  crearHoras,
  diasDelMes,
  horaTexto,
  inicioDelMes,
  sumarMinutos,
} from '../../lib/fechas';
import {
  esRangoValido,
  estaCerrado,
  limiteHoraInicio,
  limiteMaximoPrestamo,
  minutosMinimos,
  normalizarFin,
  normalizarInicio,
  siguienteBloque,
} from '../../lib/horario-reserva';
import {
  cubreCarrito,
  etiquetaMes,
  formatearFecha,
  gruposSolicitados,
  maximoDiasPrestamo,
} from '../../lib/carrito-disponibilidad';

type CampoFecha = 'inicio' | 'fin';

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
    this.consultar();
  }
  @Input() fechaInicioSeleccionada: WritableSignal<Date | null> = signal(null);
  @Input() fechaFinSeleccionada: WritableSignal<Date | null> = signal(null);
  @Input() soloAvisoFechasOcupadas = false;
  @Input() mostrarEncabezado = true;
  @Output() avisarDisponibilidad = new EventEmitter<Date>();

  carrito: Carrito = {};
  disponibilidad: Disponibilidad[] = [];
  disponibilidadDias = new Map<string, boolean>();
  cargando = false;
  consultado = false;
  campoActivo: CampoFecha = 'inicio';
  mesVisible = inicioDelMes(new Date());
  error: WritableSignal<boolean> = signal(false);
  mensajeerror = 'No se pudo consultar la disponibilidad.';
  horas: HoraOpcion[] = [];
  private minimoInicio!: Date;
  private disponibilidadRequestId = 0;
  private calendarioRequestId = 0;

  constructor(
    private readonly apiDisponibilidad: DisponibilidadService,
    private readonly configuracionService: ConfiguracionService,
  ) {}

  ngOnInit(): void {
    this.minimoInicio = siguienteBloque(this.config, new Date());
    this.horas = crearHoras();
    this.inicializarRango();
    this.mesVisible = inicioDelMes(
      this.fechaInicioSeleccionada() ?? this.minimoInicio,
    );
    this.consultar();
  }

  get diasDelMes(): Array<Date | null> {
    return diasDelMes(this.mesVisible);
  }

  get etiquetaMes(): string {
    return etiquetaMes(this.mesVisible);
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
    return maximoDiasPrestamo(this.carrito);
  }

  get mensajeRangoInvalido(): string {
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();
    const maximoDias = this.maximoDiasPrestamo;

    if ((inicio && this.esDomingo(inicio)) || (fin && this.esDomingo(fin)))
      return 'No hay atención en la fecha seleccionada.';

    if (
      inicio &&
      fin &&
      maximoDias != null &&
      fin.getTime() - inicio.getTime() > maximoDias * MILLISECONDS_PER_DAY
    )
      return `El préstamo no puede superar ${maximoDias} día(s) para los equipos seleccionados.`;

    return 'Elige una devolución de al menos 30 minutos después del inicio.';
  }

  get hayDisponibilidad(): boolean {
    return (
      this.consultado &&
      this.rangoValido &&
      cubreCarrito(this.carrito, this.disponibilidad)
    );
  }

  get cantidadDisponible(): number {
    if (this.disponibilidad.length !== 1) return 0;
    return this.disponibilidad[0].CantidadDisponible;
  }

  get hayDiasNoDisponibles(): boolean {
    return [...this.disponibilidadDias.values()].some(
      (disponible) => !disponible,
    );
  }

  get horasDisponibles(): HoraOpcion[] {
    const fecha = this.obtenerFecha(this.campoActivo) ?? this.minimoInicio;
    const horaSeleccionada = horaTexto(fecha);
    const inicioHorario = this.horario(fecha).InicioMinutos;
    const intervalo = this.minutosMinimos;
    return this.horas.filter((hora) => {
      const [h, m] = hora.value.split(':').map(Number);
      return (
        ((h * 60 + m - inicioHorario) % intervalo === 0 ||
          hora.value === horaSeleccionada) &&
        !this.horaDeshabilitada(this.campoActivo, hora.value)
      );
    });
  }

  seleccionarCampo(campo: CampoFecha): void {
    this.campoActivo = campo;
    const fecha = this.obtenerFecha(campo);
    if (fecha) this.mesVisible = inicioDelMes(fecha);
  }

  cambiarMes(desplazamiento: number): void {
    const siguienteMes = new Date(
      this.mesVisible.getFullYear(),
      this.mesVisible.getMonth() + desplazamiento,
      1,
    );

    if (comparaSoloFecha(siguienteMes, inicioDelMes(this.minimoInicio)) < 0)
      return;

    this.mesVisible = siguienteMes;
    this.consultarDisponibilidadMes();
  }

  puedeRetrocederMes(): boolean {
    return (
      comparaSoloFecha(this.mesVisible, inicioDelMes(this.minimoInicio)) > 0
    );
  }

  seleccionarDia(dia: Date): void {
    if (this.esDiaDeshabilitado(dia)) return;

    const fechaActual =
      this.obtenerFecha(this.campoActivo) ?? this.minimoInicio;
    this.asignarFecha(this.campoActivo, combinarDiaYHora(dia, fechaActual));
  }

  cambiarHora(campo: CampoFecha, valor: string): void {
    const [horas, minutos] = valor.split(':').map(Number);
    const fechaConHora = new Date(
      this.obtenerFecha(campo) ?? this.minimoInicio,
    );
    fechaConHora.setHours(horas, minutos, 0, 0);
    this.asignarFecha(campo, fechaConHora);
  }

  esDiaDeshabilitado(dia: Date): boolean {
    if (this.esDomingo(dia)) return true;

    const minimo =
      this.campoActivo === 'inicio'
        ? this.minimoInicio
        : (this.fechaInicioSeleccionada() ?? this.minimoInicio);
    if (comparaSoloFecha(dia, minimo) < 0) return true;

    const limiteMaximo = this.limiteMaximoPrestamo();
    const excedeLimite =
      this.campoActivo === 'fin' &&
      limiteMaximo != null &&
      comparaSoloFecha(dia, limiteMaximo) > 0;
    return excedeLimite || this.esDiaSinDisponibilidad(dia);
  }

  esDiaSinDisponibilidad(dia: Date): boolean {
    return (
      this.campoActivo === 'inicio' &&
      this.disponibilidadDias.get(claveFecha(dia)) === false
    );
  }

  esDiaSeleccionado(dia: Date, campo: CampoFecha): boolean {
    const fecha = this.obtenerFecha(campo);
    return !!fecha && comparaSoloFecha(dia, fecha) === 0;
  }

  esDiaEnRango(dia: Date): boolean {
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();
    if (!inicio || !fin) return false;

    return comparaSoloFecha(dia, inicio) > 0 && comparaSoloFecha(dia, fin) < 0;
  }

  esHoy(dia: Date): boolean {
    return comparaSoloFecha(dia, new Date()) === 0;
  }

  horaSeleccionada(campo: CampoFecha): string {
    return horaTexto(this.obtenerFecha(campo) ?? this.minimoInicio);
  }

  horaDeshabilitada(campo: CampoFecha, hora: string): boolean {
    const [horas, minutos] = hora.split(':').map(Number);
    const fecha = new Date(this.obtenerFecha(campo) ?? this.minimoInicio);
    fecha.setHours(horas, minutos, 0, 0);
    const horario = this.horario(fecha);
    const limite =
      campo === 'inicio' ? this.limiteHoraInicio(fecha) : horario.FinMinutos;
    const minutosHora = horas * 60 + minutos;
    if (
      !horario.Abierto ||
      minutosHora < horario.InicioMinutos ||
      minutosHora > limite
    )
      return true;

    const minimo = campo === 'inicio' ? this.minimoInicio : this.minimoFin();
    if (fecha.getTime() < minimo.getTime()) return true;

    const limiteMaximo = this.limiteMaximoPrestamo();
    return (
      campo === 'fin' &&
      limiteMaximo != null &&
      fecha.getTime() > limiteMaximo.getTime()
    );
  }

  formatearFecha(fecha: Date | null): string {
    return formatearFecha(fecha);
  }

  solicitarAviso(): void {
    const inicio = this.fechaInicioSeleccionada();
    if (inicio) this.avisarDisponibilidad.emit(inicio);
  }

  private get minutosMinimos(): number {
    return minutosMinimos(this.config);
  }

  private minimoFin(): Date {
    return sumarMinutos(
      this.fechaInicioSeleccionada() ?? this.minimoInicio,
      this.minutosMinimos,
    );
  }

  private asignarFecha(campo: CampoFecha, fecha: Date): void {
    if (campo === 'inicio') {
      const inicio = this.normalizarInicio(fecha);
      this.fechaInicioSeleccionada.set(inicio);
      this.ajustarFinAlInicio(inicio);
    } else {
      this.fechaFinSeleccionada.set(
        this.normalizarFin(fecha, this.minimoFin()),
      );
    }

    this.consultar();
  }

  private inicializarRango(): void {
    if (this.fechaInicioSeleccionada() && this.fechaFinSeleccionada()) return;

    const inicio = this.minimoInicio;
    this.fechaInicioSeleccionada.set(inicio);
    this.fechaFinSeleccionada.set(sumarMinutos(inicio, this.minutosMinimos));
  }

  private consultar(): void {
    this.consultarDisponibilidad();
    this.consultarDisponibilidadMes();
  }

  private consultarDisponibilidad(): void {
    const requestId = ++this.disponibilidadRequestId;
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
        if (requestId !== this.disponibilidadRequestId) return;
        this.disponibilidad = data;
        this.consultado = true;
        this.cargando = false;
      },
      error: (error) => {
        if (requestId !== this.disponibilidadRequestId) return;
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

  private consultarDisponibilidadMes(): void {
    const requestId = ++this.calendarioRequestId;
    const inicio = this.fechaInicioSeleccionada();
    const fin = this.fechaFinSeleccionada();
    const grupos = gruposSolicitados(this.carrito);
    if (!inicio || !fin || !this.rangoValido || grupos.length === 0) {
      this.disponibilidadDias.clear();
      return;
    }

    const dias = this.diasDelMes.filter((dia): dia is Date => dia !== null);
    if (dias.length === 0) return;

    this.disponibilidadDias.clear();

    this.apiDisponibilidad
      .obtenerDisponibilidadCalendario(
        inicio,
        fin,
        dias[0],
        dias[dias.length - 1],
        grupos,
      )
      .subscribe({
        next: (respuesta) => {
          if (requestId !== this.calendarioRequestId) return;
          this.actualizarDisponibilidadDias(respuesta);
        },
        error: () => {
          if (requestId !== this.calendarioRequestId) return;
          this.disponibilidadDias.clear();
        },
      });
  }

  private actualizarDisponibilidadDias(dias: DisponibilidadDia[]): void {
    this.disponibilidadDias = new Map(
      dias.map((dia) => [claveFecha(dia.Fecha), dia.Disponible]),
    );
  }

  private normalizarInicio(fecha: Date): Date {
    return normalizarInicio(
      this.config,
      fecha,
      this.minimoInicio,
      this.fechaFinSeleccionada(),
    );
  }

  private normalizarFin(fecha: Date, minimo: Date): Date {
    return normalizarFin(
      this.config,
      fecha,
      minimo,
      this.limiteMaximoPrestamo(),
    );
  }

  private esRangoValido(inicio: Date, fin: Date): boolean {
    return esRangoValido(this.config, inicio, fin, this.maximoDiasPrestamo);
  }

  private limiteMaximoPrestamo(): Date | null {
    return limiteMaximoPrestamo(
      this.fechaInicioSeleccionada(),
      this.maximoDiasPrestamo,
    );
  }

  private ajustarFinAlInicio(inicio: Date): void {
    const minimo = sumarMinutos(inicio, this.minutosMinimos);
    const fin = this.fechaFinSeleccionada() ?? minimo;
    this.fechaFinSeleccionada.set(this.normalizarFin(fin, minimo));
  }

  private ajustarFinAlLimite(): void {
    const inicio = this.fechaInicioSeleccionada();
    if (inicio) this.ajustarFinAlInicio(inicio);
  }

  private limiteHoraInicio(fecha: Date): number {
    return limiteHoraInicio(this.config, fecha, this.fechaFinSeleccionada());
  }

  private get config() {
    return this.configuracionService.configuracionActual();
  }

  private horario(fecha: Date) {
    return horarioParaFecha(this.config, fecha);
  }

  private esDomingo(fecha: Date): boolean {
    return estaCerrado(this.config, fecha);
  }
}
