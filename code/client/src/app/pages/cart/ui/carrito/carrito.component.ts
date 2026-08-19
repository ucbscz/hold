import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  OnDestroy,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Carrito } from '@entities/cart';
import { PrestamosAPIService } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { CalendarioComponent } from '@features/availability-selector';
import { CarritoService, CartDateValidationService } from '@features/cart';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  EquipmentImagePlaceholderComponent,
  MostrarerrorComponent,
  OpcionSelect,
  PantallaCargaComponent,
} from '@shared/ui';
import { finalize, Subscription } from 'rxjs';

@Component({
  selector: 'app-carrito',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    PantallaCargaComponent,
    CalendarioComponent,
    EquipmentImagePlaceholderComponent,
    CustomSelectComponent,
  ],
  templateUrl: './carrito.component.html',
  styleUrl: './carrito.component.css',
})
export class CarritoComponent implements OnDestroy {
  public step: number = 1;
  public errorSolicitudVisible: WritableSignal<boolean> = signal(false);
  public mensajeError: string = 'Datos insertados no validos';
  confirmacionVisible: WritableSignal<boolean> = signal(false);
  exitoVisible: WritableSignal<boolean> = signal(false);
  fechaInicio: WritableSignal<Date | null> = signal(null);
  fechaFinal: WritableSignal<Date | null> = signal(null);
  carrito: Carrito = {};
  cargando = false;
  puedeReservar: WritableSignal<boolean> = signal(true);
  motivoBloqueo: WritableSignal<string> = signal('');

  private readonly fechaActual: Date = new Date();
  private readonly imagenesFallidas = new Set<string>();
  private readonly carritoSubscription: Subscription;

  readonly validacionFechas = computed(() =>
    this.cartDateValidationService.validate(
      this.fechaInicio(),
      this.fechaFinal(),
      this.fechaActual,
    ),
  );

  constructor(
    private readonly carritoService: CarritoService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly usuarioService: UsuarioService,
    private readonly prestamosApiService: PrestamosAPIService,
    private readonly cartDateValidationService: CartDateValidationService,
  ) {
    this.carrito = this.carritoService.obtenerCarrito();
    this.carritoSubscription = this.carritoService.carrito$.subscribe(
      (carrito) => (this.carrito = carrito),
    );
    this.route.queryParams.subscribe((params) => {
      this.step = params['step'] ? Number(params['step']) : 1;
    });

    if (!this.usuarioService.estaVacio()) {
      this.prestamosApiService.estadoReserva().subscribe({
        next: (estado) => {
          this.puedeReservar.set(estado.puedeReservar);
          this.motivoBloqueo.set(estado.motivo ?? '');
        },
        error: () => {},
      });
    }
  }

  nextStep(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { step: 2 },
      queryParamsHandling: 'merge',
    });
  }

  confirmarSolicitud(): void {
    const validacion = this.validacionFechas();

    if (!validacion.isValid) {
      this.mensajeError =
        validacion.message ??
        'Selecciona una fecha de inicio y una fecha final';
      this.errorSolicitudVisible.set(true);

      return;
    }

    if (this.usuarioService.estaVacio()) {
      this.router.navigate(['/login']);

      return;
    }

    this.guardarFechasEnCarrito();

    const monto = this.carritoService.calcularPrecioTotal();

    if (monto >= 1000) {
      this.router.navigate(['/reserva']);

      return;
    }

    this.confirmacionVisible.set(true);
  }

  realizarPrestamo(): void {
    this.cargando = true;
    const carnet = this.usuarioService.obtenerUsuario().carnet!;

    this.prestamosApiService
      .crearPrestamo(this.carrito, carnet, null)
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: () => {
          this.carritoService.vaciarCarrito();
          this.exitoVisible.set(true);
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error, 'Error desconocido');
          this.mensajeError = errorMsg;
          this.errorSolicitudVisible.set(true);
        },
      });
  }

  redirigirHome(): void {
    this.router.navigate(['/inicio']);
  }

  carritoEstaVacio(): boolean {
    return Object.keys(this.carrito).length === 0;
  }

  botonDeshabilitado(): boolean {
    if (this.carritoEstaVacio()) return true;
    if (!this.puedeReservar()) return true;

    return !this.validacionFechas().isValid;
  }

  generarCantidadesMax(cantidad: number): number[] {
    return Array.from({ length: cantidad }, (_, i) => i + 1);
  }

  opcionesCantidad(cantidad: number): OpcionSelect[] {
    return this.generarCantidadesMax(cantidad).map((value) => ({
      value,
      label: String(value),
    }));
  }

  obtenerImagenProducto(imagen: string | null | undefined): string | null {
    const imageUrl = imagen?.trim();

    if (!imageUrl) return null;
    if (this.imagenesFallidas.has(imageUrl)) return null;

    return imageUrl;
  }

  registrarImagenFallida(imagen: string): void {
    const imageUrl = imagen.trim();

    if (!imageUrl) return;

    this.imagenesFallidas.add(imageUrl);
  }

  cambiarCantidad(key: string, n: number): void {
    this.carritoService.editarCantidad(Number(key), Number(n));
    this.carrito = { ...this.carritoService.obtenerCarrito() };
  }

  ngOnDestroy(): void {
    this.carritoSubscription.unsubscribe();
  }

  private guardarFechasEnCarrito(): void {
    const inicio = this.fechaInicio();
    const fin = this.fechaFinal();

    if (!inicio || !fin) return;

    this.carritoService.actualizarFechas(
      inicio.toISOString(),
      fin.toISOString(),
    );
  }
}
