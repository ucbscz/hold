import { Component, OnInit, signal } from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Router } from '@angular/router';
import { CarreraService } from '@entities/career';
import { ConfiguracionService } from '@entities/configuracion';
import { PrestamosAPIService } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { CarritoService, LoanReturnNavigationService } from '@features/cart';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
  PantallaCargaComponent,
} from '@shared/ui';
import { finalize } from 'rxjs';

type DestinoPrestamo = 'Universidad' | 'Casa' | 'Clase';

@Component({
  selector: 'app-reservation-purpose',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
    MostrarerrorComponent,
    PantallaCargaComponent,
  ],
  templateUrl: './reservation-purpose.component.html',
  styleUrl: './reservation-purpose.component.css',
})
export class ReservationPurposeComponent implements OnInit {
  readonly errorVisible = signal(false);
  readonly confirmacionVisible = signal(false);
  readonly exitoVisible = signal(false);
  readonly cargando = signal(false);
  readonly esDocente: boolean;
  destinoPrestamo: DestinoPrestamo = 'Universidad';
  carrerasOpciones: OpcionSelect[] = [];
  idCarrera: number | null = null;
  nombreCarrera = '';
  nombreMateria = '';
  mensajeError = 'No se pudo procesar la solicitud.';

  constructor(
    private readonly carritoService: CarritoService,
    private readonly usuarioService: UsuarioService,
    private readonly prestamosApiService: PrestamosAPIService,
    private readonly configuracionService: ConfiguracionService,
    private readonly carreraService: CarreraService,
    private readonly loanReturnNavigation: LoanReturnNavigationService,
    private readonly router: Router,
  ) {
    this.esDocente =
      this.usuarioService.obtenerUsuario().rol?.toLowerCase() === 'docente';
  }

  ngOnInit(): void {
    if (!this.flujoDisponible()) {
      void this.router.navigate(['/carrito'], { queryParams: { step: 2 } });
      return;
    }

    if (this.esDocente) {
      this.carreraService.obtenerCarreras().subscribe({
        next: (carreras) => {
          this.carrerasOpciones = carreras.map((carrera) => ({
            value: carrera.Id,
            label: carrera.Nombre ?? 'Carrera sin nombre',
          }));
        },
        error: () => {
          this.mensajeError = 'No se pudieron cargar las carreras.';
          this.errorVisible.set(true);
        },
      });
    }
  }

  seleccionarDestino(destino: DestinoPrestamo): void {
    this.destinoPrestamo = destino;
    if (destino === 'Casa') {
      this.idCarrera = null;
      this.nombreCarrera = '';
      this.nombreMateria = '';
    }
  }

  actualizarCarrera(id: number | null): void {
    this.idCarrera = id == null ? null : Number(id);
    this.nombreCarrera =
      this.carrerasOpciones.find(
        (opcion) => Number(opcion.value) === this.idCarrera,
      )?.label ?? '';
  }

  continuar(): void {
    if (!this.validarUsoInterno()) return;
    if (!this.seleccionValida()) {
      this.mensajeError =
        'Selecciona una carrera e ingresa la materia para continuar.';
      this.errorVisible.set(true);
      return;
    }

    const montoMinimo =
      this.configuracionService.configuracionActual()?.MontoMinimoContrato ??
      2000;

    if (this.carritoService.calcularPrecioTotal() >= montoMinimo) {
      void this.router.navigate(['/reserva'], {
        queryParams: {
          destino: this.destinoParaSolicitud(),
          idCarrera: this.idCarrera,
          nombreCarrera: this.nombreCarrera || null,
          nombreMateria: this.nombreMateria.trim() || null,
        },
      });
      return;
    }

    this.confirmacionVisible.set(true);
  }

  realizarPrestamo(): void {
    if (!this.validarUsoInterno()) return;
    const carnet = this.usuarioService.obtenerUsuario().carnet;
    if (!carnet) {
      void this.router.navigate(['/login']);
      return;
    }

    this.confirmacionVisible.set(false);
    this.cargando.set(true);
    this.prestamosApiService
      .crearPrestamo(
        this.carritoService.obtenerCarrito(),
        carnet,
        undefined,
        this.destinoParaSolicitud(),
        this.idCarrera ?? undefined,
        this.nombreMateria.trim() || undefined,
      )
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: () => {
          this.carritoService.vaciarCarrito();
          this.exitoVisible.set(true);
        },
        error: (error) => {
          this.mensajeError = extractErrorMessage(
            error,
            'No se pudo crear el préstamo.',
          );
          this.errorVisible.set(true);
        },
      });
  }

  volver(): void {
    void this.router.navigate(['/carrito'], { queryParams: { step: 2 } });
  }

  private validarUsoInterno(): boolean {
    const inicio = this.carritoService.obtenerFechaInicio();
    const fin = this.carritoService.obtenerFechaFinal();
    const fechaBolivia = (fecha: string | Date) =>
      new Intl.DateTimeFormat('en-CA', { timeZone: 'America/La_Paz' }).format(
        new Date(fecha),
      );
    if (
      this.destinoPrestamo !== 'Casa' &&
      inicio &&
      fin &&
      fechaBolivia(inicio) !== fechaBolivia(fin)
    ) {
      this.mensajeError =
        'El uso interno requiere devolver los equipos el mismo día. Vuelve al horario para ajustar la devolución.';
      this.errorVisible.set(true);
      return false;
    }
    return true;
  }

  redirigirAnterior(): void {
    void this.loanReturnNavigation.returnToPreviousPage();
  }

  seleccionValida(): boolean {
    return (
      (!this.idCarrera && !this.nombreMateria.trim()) ||
      (this.idCarrera !== null && this.nombreMateria.trim().length > 0)
    );
  }

  private destinoParaSolicitud(): DestinoPrestamo {
    return this.destinoPrestamo === 'Universidad' &&
      this.idCarrera &&
      this.nombreMateria.trim()
      ? 'Clase'
      : this.destinoPrestamo;
  }

  private flujoDisponible(): boolean {
    return (
      Object.keys(this.carritoService.obtenerCarrito()).length > 0 &&
      this.carritoService.obtenerFechaInicio() !== null &&
      this.carritoService.obtenerFechaFinal() !== null
    );
  }
}
