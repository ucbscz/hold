import {
  Directive,
  Input,
  OnChanges,
  signal,
  SimpleChanges,
  WritableSignal,
} from '@angular/core';
import { PrestamoDto } from '@entities/admin';
import { PrestamoAgrupados, PrestamosAPIService } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';
@Directive()
export abstract class HistorialBase implements OnChanges {
  @Input() filtroTexto: string = '';
  @Input() fechaDesde: string = '';
  @Input() fechaHasta: string = '';
  @Input() refreshTrigger = 0;
  datos = new Map<number, PrestamoAgrupados>();
  itemSeleccionado: PrestamoDto | null = null;
  prestamosVista: PrestamoDto[] = [];
  abrirVistaPrestamos: boolean = false;
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = '';
  exito: WritableSignal<boolean> = signal(false);
  mensajeexito: string = '';
  paginaActual = 1;
  readonly tamanoPagina = 6;
  protected abstract estado: string;

  keepOrder = (_a: unknown, _b: unknown): number => 0;
  constructor(
    protected prestamoApi: PrestamosAPIService,
    protected usuario: UsuarioService,
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['filtroTexto'] ||
      changes['fechaDesde'] ||
      changes['fechaHasta']
    ) {
      this.paginaActual = 1;
    }

    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.cargarDatos();
    }
  }
  get datosFiltrados(): Map<number, PrestamoAgrupados> {
    const texto = this.filtroTexto.trim().toLowerCase();
    const desde = this.fechaDesde ? new Date(this.fechaDesde) : null;
    const hasta = this.fechaHasta
      ? new Date(this.fechaHasta + 'T23:59:59')
      : null;
    if (!texto && !desde && !hasta) return this.datos;
    const filtrado = new Map<number, PrestamoAgrupados>();
    for (const [id, grupo] of this.datos) {
      if (texto) {
        const matchId = id.toString().includes(texto);
        const matchNombre = (grupo.datosgrupo.NombreGrupoEquipo ?? '')
          .toLowerCase()
          .includes(texto);
        if (!matchId && !matchNombre) continue;
      }
      if (desde || hasta) {
        const fecha = grupo.datosgrupo.FechaSolicitud
          ? new Date(grupo.datosgrupo.FechaSolicitud)
          : null;
        if (!fecha) continue;
        if (desde && fecha < desde) continue;
        if (hasta && fecha > hasta) continue;
      }
      filtrado.set(id, grupo);
    }
    return filtrado;
  }
  get totalRegistros(): number {
    return this.datosFiltrados.size;
  }
  get datosPaginados(): Map<number, PrestamoAgrupados> {
    const inicio = (this.paginaActual - 1) * this.tamanoPagina;
    return new Map(
      Array.from(this.datosFiltrados.entries()).slice(
        inicio,
        inicio + this.tamanoPagina,
      ),
    );
  }
  cambiarPagina(pagina: number): void {
    this.paginaActual = pagina;

    if (typeof document === 'undefined') return;
    const reducirMovimiento = window.matchMedia(
      '(prefers-reduced-motion: reduce)',
    ).matches;
    document.querySelector('.items-wrapper')?.scrollIntoView({
      behavior: reducirMovimiento ? 'auto' : 'smooth',
      block: 'start',
    });
  }
  cargarDatos() {
    if (!this.usuario.estaVacio()) {
      this.prestamoApi
        .obtenerPrestamosPorUsuario(
          this.usuario.obtenerUsuario().id!,
          this.estado,
        )
        .subscribe({
          next: (data) => {
            this.agruparPrestamos(data);
          },
          error: (error) => {
            const errorMsg = extractErrorMessage(
              error,
              'Error al cargar los prestamos, intente mas tarde',
            );
            this.mensajeerror = errorMsg;
            this.error.set(true);
          },
        });
    }
  }
  agruparPrestamos(datos: PrestamoDto[]) {
    const nuevo = new Map<number, PrestamoAgrupados>();
    for (const prestamo of datos) {
      if (nuevo.has(prestamo.Id!)) {
        nuevo.get(prestamo.Id)!.insertarEquipo(prestamo);
      } else {
        nuevo.set(prestamo.Id!, new PrestamoAgrupados([prestamo]));
      }
    }
    this.datos = nuevo;
    const totalPaginas = Math.max(
      1,
      Math.ceil(this.totalRegistros / this.tamanoPagina),
    );
    this.paginaActual = Math.min(this.paginaActual, totalPaginas);
  }
  abrirVista(item: PrestamoDto[]) {
    this.prestamosVista = item;
    this.abrirVistaPrestamos = true;
  }
  cerrarVista() {
    this.abrirVistaPrestamos = false;
    this.prestamosVista = [];
  }
}
