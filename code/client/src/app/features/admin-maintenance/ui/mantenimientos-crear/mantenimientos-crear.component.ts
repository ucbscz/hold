import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { EmpresaMantenimiento, Mantenimientos } from '@entities/admin';
import { MantenimientoService } from '@entities/maintenance';
import { EmpresamantenimientoService } from '@entities/maintenance-company';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { FlatpickrDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
import { Options } from 'flatpickr/dist/types/options';
import { MantenimientosServiceEquipos } from '../../model/mantenimientos-equipos.service';
import { ListaEquipoComponent } from './lista-equipo/lista-equipo.component';
@Component({
  selector: 'app-mantenimientos-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    ListaEquipoComponent,
    CommonModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
    FlatpickrDirective,
  ],
  templateUrl: './mantenimientos-crear.component.html',
  styleUrl: './mantenimientos-crear.component.css',
})
export class MantenimientosCrearComponent extends BaseTablaComponent {
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  agregarequipo: WritableSignal<boolean> = signal(false);
  equiposExpandidos = false;
  fechaminima = this.toLocalISOString(new Date());
  opcionesFechaInicio: Partial<Options> = { minDate: this.fechaminima };
  opcionesFechaFinal: Partial<Options> = { minDate: this.fechaminima };
  readonly tipoOpciones: OpcionSelect[] = [
    { value: 'preventivo', label: 'Preventivo' },
    { value: 'correctivo', label: 'Correctivo' },
  ];
  mantenimiento: Mantenimientos = new Mantenimientos();
  empresas: string[] = [];
  mantenimientoSeleccionado: Map<
    number,
    {
      TipoMantenimiento: string;
      DescripcionEquipo: string;
      nombre: string;
    }
  > = new Map();
  constructor(
    private readonly mantenimientoapi: MantenimientoService,
    private mantenimientoequipo: MantenimientosServiceEquipos,
    private empresa: EmpresamantenimientoService,
  ) {
    super();
  }
  ngOnInit() {
    this.obtenermantenimientoSeleccionado();
    this.obtenereempresasMantenimiento();
  }
  ngOnDestroy() {
    this.mantenimientoequipo.vaciarEquiposMantenimientos();
  }
  validarFecha() {
    if (
      !this.mantenimiento.FechaMantenimiento ||
      !this.mantenimiento.FechaFinalDeMantenimiento
    ) {
      return false;
    }
    const fechaInicio = new Date(this.mantenimiento.FechaMantenimiento);
    const fechaFinal = new Date(this.mantenimiento.FechaFinalDeMantenimiento);
    const fechaMinima = new Date(this.fechaminima);
    if (fechaInicio > fechaFinal) {
      return false;
    }
    if (fechaInicio < fechaMinima) {
      return false;
    }
    return true;
  }

  onFechaMantenimiento(dates: Date[]): void {
    this.mantenimiento.FechaMantenimiento = dates[0] ?? null;
    this.opcionesFechaFinal = {
      minDate: this.mantenimiento.FechaMantenimiento ?? this.fechaminima,
      maxDate: this.fechamaxima(this.mantenimiento.FechaMantenimiento),
    };

    if (!this.validarFechaFinal()) {
      this.mantenimiento.FechaFinalDeMantenimiento = null;
    }
  }

  onFechaFinalDeMantenimiento(dates: Date[]): void {
    this.mantenimiento.FechaFinalDeMantenimiento = dates[0] ?? null;
  }

  fechamaxima(fecha: Date | null): Date | undefined {
    if (!fecha) return undefined;

    const fechaMaxima = new Date(fecha);
    fechaMaxima.setFullYear(fechaMaxima.getFullYear() + 1);
    return fechaMaxima;
  }

  private validarFechaFinal(): boolean {
    if (
      !this.mantenimiento.FechaMantenimiento ||
      !this.mantenimiento.FechaFinalDeMantenimiento
    )
      return true;

    return (
      new Date(this.mantenimiento.FechaFinalDeMantenimiento) >=
      new Date(this.mantenimiento.FechaMantenimiento)
    );
  }
  obtenereempresasMantenimiento() {
    this.empresa.obtenerEmpresaMantenimiento().subscribe({
      next: (empresas: EmpresaMantenimiento[]) => {
        this.empresas = empresas.map((empresa) => empresa.NombreEmpresa ?? '');
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar las empresas de mantenimiento. Por favor, inténtelo de nuevo más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  agregarEquipo() {
    this.agregarequipo.set(!this.agregarequipo());
  }
  obtenermantenimientoSeleccionado() {
    this.mantenimientoSeleccionado =
      this.mantenimientoequipo.obtenerEquiposMantenimientos();
  }
  eliminarEquipo(codigo: number) {
    this.mantenimientoequipo.quitarEquipo(codigo);
    this.obtenermantenimientoSeleccionado();
  }
  actualizarTipoEquipo(codigo: number, tipo: string): void {
    this.mantenimientoequipo.actualizarTipo(codigo, tipo);
    this.obtenermantenimientoSeleccionado();
  }
  alternarEquiposSeleccionados(): void {
    this.equiposExpandidos = !this.equiposExpandidos;
  }
  validarcreacion() {
    this.mensajeaviso = '¿Está seguro de que desea crear este mantenimiento?';
    this.aviso.set(true);
  }
  registrar() {
    const mantenimientoParaEnvio = {
      FechaMantenimiento: this.mantenimiento.FechaMantenimiento,
      FechaFinalDeMantenimiento: this.mantenimiento.FechaFinalDeMantenimiento,
      NombreEmpresaMantenimiento: this.mantenimiento.NombreEmpresaMantenimiento,
      Costo: this.mantenimiento.Costo,
      DescripcionMantenimiento: this.mantenimiento.Descripcion,
    };
    this.mantenimientoapi
      .crearMantenimiento(
        mantenimientoParaEnvio,
        this.mantenimientoSeleccionado,
      )
      .subscribe({
        next: (_response) => {
          this.Actualizar.emit();
          this.mensajeexito = 'Mantenimiento creado con éxito.';
          this.exito.set(true);
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al crear el mantenimiento. Por favor, inténtelo de nuevo más tarde.',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
  toLocalISOString(date: Date): string {
    const offset = date.getTimezoneOffset();
    const localDate = new Date(date.getTime() - offset * 60000);
    return localDate.toISOString().split('T')[0];
  }
}
