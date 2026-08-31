import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ConfiguracionDto,
  HorarioAtencion,
  ConfiguracionService,
} from '@entities/configuracion';
import { FirmaComponent } from '@features/signature';
import { CustomSelectComponent } from '@shared/ui';
import { FlatpickrDirective } from '@shared/lib/directives';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-configuraciones',
  standalone: true,
  imports: [
    FormsModule,
    FirmaComponent,
    CustomSelectComponent,
    FlatpickrDirective,
  ],
  templateUrl: './admin-configuraciones.component.html',
  styleUrls: ['./admin-configuraciones.component.css'],
})
export class AdminConfiguracionesComponent implements OnInit {
  readonly clickfirma = signal(false);
  readonly config: WritableSignal<ConfiguracionDto | null> = signal(null);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly mensaje = signal('');
  readonly error = signal(false);
  horarioInicioHora = 8;
  horarioInicioMinuto = 0;
  horarioFinHora = 18;
  horarioFinMinuto = 0;
  readonly dias = [
    'Domingo',
    'Lunes',
    'Martes',
    'Miércoles',
    'Jueves',
    'Viernes',
    'Sábado',
  ];
  readonly horas = Array.from({ length: 48 }, (_, i) => ({
    value: i * 30,
    label: `${String(Math.floor(i / 2)).padStart(2, '0')}:${i % 2 ? '30' : '00'}`,
  }));
  horarios: HorarioAtencion[] = [];
  fechaEspecial = '';

  aplicarSemana(): void {
    this.horarios = this.horarios.map((h) =>
      h.Fecha
        ? h
        : {
            ...h,
            InicioMinutos:
              this.horarioInicioHora * 60 + this.horarioInicioMinuto,
            FinMinutos: this.horarioFinHora * 60 + this.horarioFinMinuto,
          },
    );
  }

  agregarExcepcion(): void {
    if (
      !this.fechaEspecial ||
      this.horarios.some((h) => h.Fecha === this.fechaEspecial)
    )
      return;
    this.horarios = [
      ...this.horarios,
      {
        Fecha: this.fechaEspecial,
        DiaSemana: 0,
        Abierto: false,
        InicioMinutos: 480,
        FinMinutos: 1080,
      },
    ];
    this.fechaEspecial = '';
  }

  quitarExcepcion(horario: HorarioAtencion): void {
    this.horarios = this.horarios.filter((h) => h !== horario);
  }

  constructor(private readonly configuracionService: ConfiguracionService) {}

  ngOnInit(): void {
    this.cargarConfiguracion();
  }

  cargarConfiguracion(): void {
    this.cargando.set(true);
    this.configuracionService
      .loadConfiguracion()
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (data) => {
          this.config.set({ ...data });
          this.parseHorarios(data);
        },
        error: () =>
          this.mostrarMensaje('Error al cargar la configuración', true),
      });
  }

  abrirFirma(): void {
    this.clickfirma.set(true);
  }

  guardarfirma(signatureData: string): void {
    this.config.update((current) =>
      current ? { ...current, FirmaJefeCarreraBase64: signatureData } : current,
    );
  }

  guardar(): void {
    const currentConfig = this.config();
    if (!currentConfig || !this.configuracionValida()) return;

    const updatedConfig = {
      ...currentConfig,
      Horarios: this.horarios,
      HorarioInicioMinutos:
        this.horarioInicioHora * 60 + this.horarioInicioMinuto,
      HorarioFinMinutos: this.horarioFinHora * 60 + this.horarioFinMinuto,
    };

    this.guardando.set(true);
    this.configuracionService
      .updateConfiguracion(updatedConfig)
      .pipe(finalize(() => this.guardando.set(false)))
      .subscribe({
        next: (data) => {
          this.config.set({ ...data });
          this.parseHorarios(data);
          this.mostrarMensaje('Configuración guardada exitosamente', false);
        },
        error: () =>
          this.mostrarMensaje('Error al guardar la configuración', true),
      });
  }

  configuracionValida(): boolean {
    const current = this.config();
    if (!current) return false;

    const inicio = this.horarioInicioHora * 60 + this.horarioInicioMinuto;
    const fin = this.horarioFinHora * 60 + this.horarioFinMinuto;

    return (
      this.horarios.every(
        (h) =>
          !h.Abierto ||
          h.FinMinutos - h.InicioMinutos >= current.TiempoMinimoReservaMinutos,
      ) &&
      inicio >= 0 &&
      fin <= 24 * 60 - 1 &&
      inicio < fin &&
      current.MontoMinimoContrato >= 0 &&
      current.TiempoMinimoReservaMinutos >= 30 &&
      current.TiempoRecordatorioPrevioMinutos >= 0 &&
      current.MinutosGraciaAtraso >= 0 &&
      current.NombreJefeCarrera.trim().length > 0
    );
  }

  private parseHorarios(data: ConfiguracionDto): void {
    this.horarios = [1, 2, 3, 4, 5, 6, 0].map((day) => ({
      ...(data.Horarios?.find((h) => !h.Fecha && h.DiaSemana === day) ?? {
        DiaSemana: day,
        Abierto: day !== 0,
        InicioMinutos: data.HorarioInicioMinutos,
        FinMinutos: data.HorarioFinMinutos,
      }),
    }));
    this.horarios.push(
      ...(data.Horarios?.filter((h) => !!h.Fecha).map((h) => ({ ...h })) ?? []),
    );
    const inicio = this.normalizarMinutos(data.HorarioInicioMinutos, 8 * 60);
    const fin = this.normalizarMinutos(data.HorarioFinMinutos, 18 * 60);

    this.horarioInicioHora = Math.floor(inicio / 60);
    this.horarioInicioMinuto = inicio % 60;
    this.horarioFinHora = Math.floor(fin / 60);
    this.horarioFinMinuto = fin % 60;
  }

  private normalizarMinutos(value: unknown, fallback: number): number {
    const numericValue = Number(value);

    if (!Number.isFinite(numericValue)) return fallback;

    return Math.min(24 * 60 - 1, Math.max(0, Math.trunc(numericValue)));
  }

  private mostrarMensaje(msg: string, esError: boolean): void {
    this.mensaje.set(msg);
    this.error.set(esError);
    window.setTimeout(() => this.mensaje.set(''), 3000);
  }
}
