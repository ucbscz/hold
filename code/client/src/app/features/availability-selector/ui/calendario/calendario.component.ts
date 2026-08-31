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
import { Disponibilidad, DisponibilidadService } from '@entities/availability';
import { Carrito } from '@entities/cart';
import { extractErrorMessage } from '@shared/lib/error';
import { CustomSelectComponent, MostrarerrorComponent } from '@shared/ui';

const MILLISECONDS_PER_MINUTE = 60 * 1000;
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
  horas: HoraOpcion[] = [];
  private minimoInicio!: Date;

  constructor(
    private readonly apiDisponibilidad: DisponibilidadService,
    private readonly configuracionService: ConfiguracionService,
  ) {}

  ngOnInit(): void {
    this.minimoInicio = this.siguienteBloque();
    this.horas = this.crearHoras();
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

    if ((inicio && this.esDomingo(inicio)) || (fin && this.esDomingo(fin)))
      return 'No hay atención en la fecha seleccionada.';

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
    const fecha = this.obtenerFecha(this.campoActivo) ?? this.minimoInicio;
    const horario = this.horario(fecha);
    const intervalo =
      this.configuracionService.configuracionActual()
        ?.TiempoMinimoReservaMinutos ?? 30;
    return this.horas.filter((hora) => {
      const [h, m] = hora.value.split(':').map(Number);
      return (
        (h * 60 + m - horario.InicioMinutos) % intervalo === 0 &&
        !this.horaDeshabilitada(this.campoActivo, hora.value)
      );
    });
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
          this.sumarMinutos(
            inicio,
            this.configuracionService.configuracionActual()
              ?.TiempoMinimoReservaMinutos ?? 30,
          ),
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
          this.sumarMinutos(
            inicio,
            this.configuracionService.configuracionActual()
              ?.TiempoMinimoReservaMinutos ?? 30,
          ),
        ),
      );
    }

    this.consultarDisponibilidad();
  }

  esDiaDeshabilitado(dia: Date): boolean {
    if (this.esDomingo(dia)) return true;

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
    const minutosDelDia = horas * 60 + minutos;
    const horario = this.horario(this.obtenerFecha(campo) ?? this.minimoInicio);
    const limite =
      horario.FinMinutos -
      (campo === 'inicio'
        ? (this.configuracionService.configuracionActual()
            ?.TiempoMinimoReservaMinutos ?? 30)
        : 0);
    if (
      !horario.Abierto ||
      minutosDelDia < horario.InicioMinutos ||
      minutosDelDia > limite
    )
      return true;

    const fecha = new Date(this.obtenerFecha(campo) ?? this.minimoInicio);
    fecha.setHours(horas, minutos, 0, 0);
    const minimo =
      campo === 'inicio'
        ? this.minimoInicio
        : this.sumarMinutos(
            this.fechaInicioSeleccionada() ?? this.minimoInicio,
            this.configuracionService.configuracionActual()
              ?.TiempoMinimoReservaMinutos ?? 30,
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
      this.sumarMinutos(
        inicio,
        this.configuracionService.configuracionActual()
          ?.TiempoMinimoReservaMinutos ?? 30,
      ),
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
    let normalizada =
      fecha.getTime() < minimo.getTime() ? new Date(minimo) : new Date(fecha);
    const apertura = this.limiteDelDia(
      normalizada,
      this.horario(normalizada).InicioMinutos,
    );
    if (normalizada < apertura) normalizada = apertura;
    const limite = this.limiteDelDia(
      normalizada,
      this.horario(normalizada).FinMinutos -
        (this.configuracionService.configuracionActual()
          ?.TiempoMinimoReservaMinutos ?? 30),
    );

    return normalizada.getTime() > limite.getTime()
      ? this.inicioDelSiguienteDia(normalizada)
      : normalizada;
  }

  private normalizarFin(fecha: Date, minimo: Date): Date {
    let normalizada =
      fecha.getTime() < minimo.getTime() ? new Date(minimo) : new Date(fecha);
    const limite = this.limiteDelDia(
      normalizada,
      this.horario(normalizada).FinMinutos,
    );

    if (normalizada.getTime() > limite.getTime()) normalizada = limite;

    const limiteMaximo = this.limiteMaximoPrestamo();
    if (limiteMaximo && normalizada.getTime() > limiteMaximo.getTime())
      normalizada = limiteMaximo;

    return normalizada;
  }

  private crearHoras(): HoraOpcion[] {
    return Array.from({ length: 1440 }, (_, minutes) => {
      const value = `${this.dosDigitos(Math.floor(minutes / 60))}:${this.dosDigitos(minutes % 60)}`;
      return { value, label: value };
    });
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
    const apertura = this.horario(inicio);
    const cierre = this.horario(fin);
    const minutosInicio = inicio.getHours() * 60 + inicio.getMinutes();
    const minutosFin = fin.getHours() * 60 + fin.getMinutes();
    const minimo =
      this.configuracionService.configuracionActual()
        ?.TiempoMinimoReservaMinutos ?? 30;
    return (
      duration >= minimo * MILLISECONDS_PER_MINUTE &&
      apertura.Abierto &&
      cierre.Abierto &&
      minutosInicio >= apertura.InicioMinutos &&
      minutosInicio <= apertura.FinMinutos - minimo &&
      minutosFin >= cierre.InicioMinutos &&
      minutosFin <= cierre.FinMinutos &&
      (this.maximoDiasPrestamo == null ||
        duration <= this.maximoDiasPrestamo * 86400000)
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
    const minimo = this.sumarMinutos(
      inicio,
      this.configuracionService.configuracionActual()
        ?.TiempoMinimoReservaMinutos ?? 30,
    );
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
    const siguiente = new Date(fecha);
    for (let intento = 0; intento < 374; intento++) {
      siguiente.setDate(siguiente.getDate() + 1);
      if (this.horario(siguiente).Abierto) break;
    }
    return this.limiteDelDia(siguiente, this.horario(siguiente).InicioMinutos);
  }

  private horario(fecha: Date) {
    return horarioParaFecha(
      this.configuracionService.configuracionActual(),
      fecha,
    );
  }

  private esDomingo(fecha: Date): boolean {
    return !this.horario(fecha).Abierto;
  }

  private dosDigitos(valor: number): string {
    return String(valor).padStart(2, '0');
  }

  private siguienteBloque(): Date {
    const fecha = new Date();
    fecha.setSeconds(0, 0);

    if (this.esDomingo(fecha)) return this.inicioDelSiguienteDia(fecha);

    if (
      fecha.getHours() * 60 + fecha.getMinutes() <
      this.horario(fecha).InicioMinutos
    ) {
      return this.limiteDelDia(fecha, this.horario(fecha).InicioMinutos);
    }

    fecha.setMinutes(
      Math.ceil(
        fecha.getMinutes() /
          (this.configuracionService.configuracionActual()
            ?.TiempoMinimoReservaMinutos ?? 30),
      ) *
        (this.configuracionService.configuracionActual()
          ?.TiempoMinimoReservaMinutos ?? 30),
    );
    if (fecha <= new Date()) {
      fecha.setMinutes(
        fecha.getMinutes() +
          (this.configuracionService.configuracionActual()
            ?.TiempoMinimoReservaMinutos ?? 30),
      );
    }
    if (
      fecha.getHours() * 60 + fecha.getMinutes() >
      this.horario(fecha).FinMinutos -
        (this.configuracionService.configuracionActual()
          ?.TiempoMinimoReservaMinutos ?? 30)
    ) {
      return this.inicioDelSiguienteDia(fecha);
    }
    return fecha;
  }
}
