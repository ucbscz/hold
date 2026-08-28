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
import {
  CustomSelectComponent,
  EquipmentImagePlaceholderComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-carrito',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MostrarerrorComponent,
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
  fechaInicio: WritableSignal<Date | null> = signal(null);
  fechaFinal: WritableSignal<Date | null> = signal(null);
  carrito: Carrito = {};
  puedeReservar: WritableSignal<boolean> = signal(true);
  motivoBloqueo: WritableSignal<string> = signal('');
  maximoDiasPrestamo: WritableSignal<number | null> = signal(null);

  private readonly fechaActual: Date = new Date();
  private readonly imagenesFallidas = new Set<string>();
  private readonly carritoSubscription: Subscription;

  readonly validacionFechas = computed(() =>
    this.cartDateValidationService.validate(
      this.fechaInicio(),
      this.fechaFinal(),
      this.fechaActual,
      this.maximoDiasPrestamo(),
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
    this.actualizarCarrito(this.carrito);
    this.carritoSubscription = this.carritoService.carrito$.subscribe(
      (carrito) => this.actualizarCarrito(carrito),
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

  previousStep(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { step: 1 },
      queryParamsHandling: 'merge',
    });
  }

  continuarAlDestino(): void {
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

    void this.router.navigate(['/destino']);
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

  private actualizarCarrito(carrito: Carrito): void {
    this.carrito = carrito;
    const limits = Object.values(carrito)
      .map((item) => item.tiempoMaximoPrestamoDias)
      .filter((days) => Number.isFinite(days) && days > 0);
    this.maximoDiasPrestamo.set(limits.length > 0 ? Math.min(...limits) : null);
  }
}
