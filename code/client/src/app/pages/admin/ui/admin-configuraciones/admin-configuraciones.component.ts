import {
  Component,
  DestroyRef,
  OnInit,
  ViewChild,
  inject,
  signal,
  WritableSignal,
} from '@angular/core';
import { NgForm } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ValidatedFormsModule } from '@shared/lib/forms';
import {
  ConfiguracionDto,
  HorarioAtencion,
  ConfiguracionService,
} from '@entities/configuracion';
import { FirmaComponent } from '@features/signature';
import {
  AvisoExitoComponent,
  CustomSelectComponent,
  OpcionSelect,
} from '@shared/ui';
import { FlatpickrDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error/error-handler';
import {
  Subject,
  catchError,
  debounceTime,
  finalize,
  of,
  switchMap,
} from 'rxjs';

@Component({
  selector: 'app-admin-configuraciones',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    FirmaComponent,
    CustomSelectComponent,
    FlatpickrDirective,
    AvisoExitoComponent,
  ],
  templateUrl: './admin-configuraciones.component.html',
  styleUrls: ['./admin-configuraciones.component.css'],
})
export class AdminConfiguracionesComponent implements OnInit {
  @ViewChild(NgForm) form?: NgForm;
  private readonly destroyRef = inject(DestroyRef);
  private readonly buscarResponsable = new Subject<string>();
  private messageTimer?: number;
  readonly responsables = signal<OpcionSelect[]>([]);
  readonly errorResponsables = signal('');
  readonly clickfirma = signal(false);
  readonly config: WritableSignal<ConfiguracionDto | null> = signal(null);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly mensaje = signal('');
  readonly error = signal(false);
  readonly exito = signal(false);
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
  readonly fechaOptions = { dateFormat: 'Y-m-d' };

  buscarUsuarios(texto: string): void {
    this.buscarResponsable.next(texto.slice(0, 100));
  }

  asignarResponsable(carnet: string): void {
    const current = this.config();
    if (!current || current.CarnetJefeCarrera === carnet) return;
    this.config.set({
      ...current,
      CarnetJefeCarrera: carnet,
      NombreJefeCarrera:
        this.responsables().find((u) => u.value === carnet)?.label ?? '',
      FirmaJefeCarreraBase64: '',
    });
  }

  get horarioBaseValido(): boolean {
    const partes = [
      this.horarioInicioHora,
      this.horarioInicioMinuto,
      this.horarioFinHora,
      this.horarioFinMinuto,
    ];
    return (
      partes.every(
        (valor, i) =>
          Number.isInteger(valor) && valor >= 0 && valor <= (i % 2 ? 59 : 23),
      ) &&
      this.horarioFinHora * 60 +
        this.horarioFinMinuto -
        this.horarioInicioHora * 60 -
        this.horarioInicioMinuto >=
        (this.config()?.TiempoMinimoReservaMinutos ?? 30)
    );
  }

  errorHorario(horario: HorarioAtencion): string {
    if (!horario.Abierto) return '';
    return horario.FinMinutos - horario.InicioMinutos >=
      (this.config()?.TiempoMinimoReservaMinutos ?? 30)
      ? ''
      : 'El cierre debe permitir al menos ' +
          (this.config()?.TiempoMinimoReservaMinutos ?? 30) +
          ' minutos de reserva.';
  }

  get fechaRepetida(): boolean {
    return this.horarios.some((h) => h.Fecha === this.fechaEspecial);
  }

  aplicarSemana(): void {
    if (!this.horarioBaseValido) return;
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
      this.horarios.length >= 373 ||
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

  constructor(private readonly configuracionService: ConfiguracionService) {
    this.destroyRef.onDestroy(() => window.clearTimeout(this.messageTimer));
  }

  ngOnInit(): void {
    this.buscarResponsable
      .pipe(
        debounceTime(200),
        switchMap((buscar) => {
          this.errorResponsables.set('');
          return this.configuracionService.buscarResponsables(buscar).pipe(
            catchError(() => {
              this.errorResponsables.set(
                'No se pudieron cargar los usuarios. Vuelve a buscar.',
              );
              return of([]);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((usuarios) => {
        const opciones = usuarios.map((u) => ({
          value: u.Carnet,
          label: u.Nombre,
        }));
        const current = this.config();
        if (
          current?.CarnetJefeCarrera &&
          !opciones.some((u) => u.value === current.CarnetJefeCarrera)
        )
          opciones.unshift({
            value: current.CarnetJefeCarrera,
            label: current.NombreJefeCarrera,
          });
        this.responsables.set(opciones);
      });
    this.cargarConfiguracion();
  }

  cargarConfiguracion(): void {
    this.cargando.set(true);
    this.configuracionService
      .loadConfiguracion(false)
      .pipe(
        finalize(() => this.cargando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (data) => {
          this.config.set({ ...data });
          this.responsables.set(
            data.CarnetJefeCarrera
              ? [
                  {
                    value: data.CarnetJefeCarrera,
                    label: data.NombreJefeCarrera,
                  },
                ]
              : [],
          );
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
    if (
      !currentConfig ||
      !this.configuracionValida() ||
      this.form?.invalid ||
      this.guardando()
    )
      return;

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
      .pipe(
        finalize(() => this.guardando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (data) => {
          this.config.set({ ...data });
          this.responsables.set(
            data.CarnetJefeCarrera
              ? [
                  {
                    value: data.CarnetJefeCarrera,
                    label: data.NombreJefeCarrera,
                  },
                ]
              : [],
          );
          this.parseHorarios(data);
          this.mostrarMensaje('Configuración guardada exitosamente', false);
        },
        error: (error) =>
          this.mostrarMensaje(
            extractErrorMessage(error, 'Error al guardar la configuración'),
            true,
          ),
      });
  }

  configuracionValida(): boolean {
    const current = this.config();
    if (!current) return false;

    const inicio = this.horarioInicioHora * 60 + this.horarioInicioMinuto;
    const fin = this.horarioFinHora * 60 + this.horarioFinMinuto;

    return (
      this.horarioBaseValido &&
      this.horarios.every((h) => !this.errorHorario(h)) &&
      inicio >= 0 &&
      fin <= 24 * 60 - 1 &&
      inicio < fin &&
      current.MontoMinimoContrato >= 0 &&
      current.TiempoMinimoReservaMinutos >= 30 &&
      current.TiempoRecordatorioPrevioMinutos >= 0 &&
      current.MinutosGraciaAtraso >= 0 &&
      !!current.CarnetJefeCarrera &&
      !!current.FirmaJefeCarreraBase64
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
    if (!esError) {
      this.mensaje.set('');
      this.error.set(false);
      this.exito.set(true);
      return;
    }
    this.mensaje.set(msg);
    this.error.set(true);
    window.clearTimeout(this.messageTimer);
    this.messageTimer = window.setTimeout(() => this.mensaje.set(''), 5000);
  }
}
