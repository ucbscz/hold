import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  inject,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  compararPrestamos,
  ESTADO_PRESTAMO_FILTRO_OPCIONES,
  PrestamoAgrupados,
  PrestamoDto,
  PrestamosAPIService,
  VercontratoComponent,
  VistaPrestamosComponent,
} from '@entities/loan';
import {
  UsuarioServiceAPI,
  UsuarioService,
  NombreRolPipe,
  nombreRol,
  ROL_OPCIONES,
  ROL_ADMINISTRADOR,
} from '@entities/user';
import { ESTADO_EQUIPO_OPCIONES } from '@entities/equipment';
import {
  Aviso,
  AvisoEliminarComponent,
  BuscadorComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
  PantallaCargaComponent,
  ToastService,
} from '@shared/ui';
import {
  AdminTabsComponent,
  Tabla,
  TablePaginationComponent,
  printTable,
} from '@shared/lib/admin-table';
import {
  FlatpickrDirective,
  StickyScrollDirective,
} from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import { formatRegionDateTime } from '@shared/lib/date';
import { AuditPanelComponent } from '@entities/audit-log';
import { finalize } from 'rxjs';
@Component({
  selector: 'app-prestamos-tabla',
  standalone: true,
  imports: [
    AdminTabsComponent,
    StickyScrollDirective,
    CommonModule,
    NombreRolPipe,
    FormsModule,
    ReactiveFormsModule,
    VercontratoComponent,
    PantallaCargaComponent,
    VistaPrestamosComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    Aviso,
    BuscadorComponent,
    AuditPanelComponent,
    CustomSelectComponent,
    TablePaginationComponent,
    FlatpickrDirective,
  ],
  templateUrl: './prestamos-tabla.component.html',
  styleUrls: [
    '../../../../shared/lib/admin-table/tabla-admin.css',
    './prestamos-tabla.component.css',
  ],
})
export class PrestamosTablaComponent extends Tabla implements OnInit {
  private readonly toast = inject(ToastService);
  readonly esRoot =
    inject(UsuarioService).obtenerUsuario().rol?.toLowerCase() ===
    ROL_ADMINISTRADOR;
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }
  cargando: boolean = false;
  alertaeliminar: boolean = false;
  prestamos: Map<number, PrestamoAgrupados> = new Map<
    number,
    PrestamoAgrupados
  >();
  prestamoscopia: Map<number, PrestamoAgrupados> = new Map<
    number,
    PrestamoAgrupados
  >();

  get prestamosTabla() {
    return Array.from(this.prestamos.entries(), ([key, value]) => ({
      key,
      value,
    }));
  }

  resumenEquipo(prestamo: PrestamoAgrupados): string {
    const primerEquipo = prestamo.equipos[0]?.NombreGrupoEquipo ?? '';

    return prestamo.equipos.length > 1 ? `${primerEquipo}...` : primerEquipo;
  }

  detalleEquipos(prestamo: PrestamoAgrupados): string {
    return prestamo.equipos
      .map((equipo) => equipo.NombreGrupoEquipo)
      .filter((nombre): nombre is string => !!nombre)
      .join(', ');
  }

  exportarPrestamos(): void {
    this.exportarCsv(
      'prestamos',
      this.columnasExportacion,
      this.filasExportacion(),
    );
  }

  imprimirPrestamos(): void {
    printTable({
      title: 'Préstamos',
      headers: this.columnasExportacion,
      rows: this.filasExportacion(),
    });
  }

  private readonly columnasExportacion = [
    'Usuario',
    'Carnet',
    'Rol',
    'Teléfono',
    'Equipos',
    'Fecha Solicitud',
    'Fecha Préstamo Esperada',
    'Fecha Devolución Esperada',
    'Estado',
  ];

  private filasExportacion() {
    return this.prestamosTabla.map(({ value }) => [
      `${value.datosgrupo.NombreUsuario ?? ''} ${value.datosgrupo.ApellidoPaternoUsuario ?? ''}`.trim(),
      value.datosgrupo.CarnetUsuario,
      nombreRol(value.datosgrupo.TipoUsuario),
      value.datosgrupo.TelefonoUsuario,
      this.detalleEquipos(value),
      formatRegionDateTime(value.datosgrupo.FechaSolicitud),
      formatRegionDateTime(value.datosgrupo.FechaPrestamoEsperada),
      formatRegionDateTime(value.datosgrupo.FechaDevolucionEsperada),
      this.getEstadoCalculado(value),
    ]);
  }

  vercontrato: WritableSignal<boolean> = signal(false);
  prestamoSeleccionado: PrestamoDto = new PrestamoDto();
  prestamoKeySeleccionado: number = 0;
  decisionPrestamo: 'aprobado' | 'rechazado' | null = null;
  detalleDecision = '';
  procesandoDecision = false;
  avisocancelar: WritableSignal<boolean> = signal(false);
  mensajeavisocancelar: string =
    '¿Está seguro de cancelar el préstamo seleccionado?';
  override columnas: string[] = [
    'Usuario',
    'Carnet',
    'Rol',
    'Teléfono',
    'Equipos',
    'Fecha Solicitud',
    'Fecha Préstamo Esperada',
    'Fecha Devolución Esperada',
  ];
  estadoSeleccionado: string = '';
  readonly estadoFiltroOpciones = ESTADO_PRESTAMO_FILTRO_OPCIONES;
  rolSeleccionado: string = '';
  readonly rolesFiltroOpciones: OpcionSelect[] = [
    { value: '', label: 'Todos los roles' },
    ...ROL_OPCIONES,
  ];
  fechaPrestamoDesde = '';
  fechaPrestamoHasta = '';
  private busquedaActual?: [string, string];
  abrirVista: boolean = false;
  observacionEditar = '';
  editandoObservacion: number | null = null;

  editarObservacion(id: number): void {
    this.editandoObservacion = id;
    this.observacionEditar =
      this.prestamos.get(id)?.datosgrupo.Observacion ?? '';
  }

  guardarObservacion(): void {
    if (this.editandoObservacion == null) return;
    this.prestamosapi
      .editarObservacion(this.editandoObservacion, this.observacionEditar)
      .subscribe({
        next: () => {
          this.editandoObservacion = null;
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (e) => {
          this.mensajeerror = extractErrorMessage(e);
          this.error.set(true);
        },
      });
  }
  prestamosVista: PrestamoDto[] = [];
  constructor(
    private readonly prestamosapi: PrestamosAPIService,
    private readonly usuarioapi: UsuarioServiceAPI,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarPrestamos();
  }
  limpiarPrestamoSeleccionado() {
    this.prestamoSeleccionado = new PrestamoDto();
  }
  cargarPrestamos() {
    this.prestamosapi.obtenerPrestamos().subscribe({
      next: (data: PrestamoDto[]) => {
        this.agruparPrestamos(data);
        this.seleccionarEstado(this.estadoSeleccionado);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los préstamos. Por favor, inténtelo de nuevo más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  agruparPrestamos(datos: PrestamoDto[]) {
    this.prestamos.clear();
    if (datos.length === 0) {
      this.prestamoscopia = new Map(this.prestamos);
      return;
    }
    for (const prestamo of [...datos].sort(compararPrestamos)) {
      if (prestamo.Id == null) continue;
      if (!this.prestamos.has(prestamo.Id)) {
        this.prestamos.set(prestamo.Id, new PrestamoAgrupados([prestamo]));
      } else {
        this.prestamos.get(prestamo.Id)!.insertarEquipo(prestamo);
      }
    }
    this.prestamoscopia = new Map(this.prestamos);
  }
  eliminarPrestamo(prestamo: PrestamoDto) {
    this.prestamoSeleccionado = prestamo;
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    this.cargando = true;
    this.prestamosapi
      .eliminarPrestamo(this.prestamoSeleccionado.Id)
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: (_response) => {
          this.toast.success('Préstamo eliminado con éxito.');
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al eliminar el préstamo. Por favor, inténtelo de nuevo más tarde.',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
    this.limpiarPrestamoSeleccionado();
    this.alertaeliminar = false;
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.limpiarPrestamoSeleccionado();
  }
  seleccionarEstado(estado: string) {
    this.estadoSeleccionado = estado;
    this.aplicarFiltros();
  }
  onFechaPrestamoDesde(dates: Date[]): void {
    this.fechaPrestamoDesde = dates[0]?.toISOString().slice(0, 10) ?? '';
    this.aplicarFiltros();
  }
  onFechaPrestamoHasta(dates: Date[]): void {
    this.fechaPrestamoHasta = dates[0]?.toISOString().slice(0, 10) ?? '';
    this.aplicarFiltros();
  }
  seleccionarRol(rol: string) {
    this.rolSeleccionado = rol;
    this.aplicarFiltros();
  }

  aplicarFiltros(event?: [string, string]) {
    if (event) this.busquedaActual = event;
    let prestamosFiltrados = Array.from(this.prestamoscopia.entries());
    const busqueda = this.busquedaActual;
    if (busqueda && busqueda[0].trim() !== '') {
      const busquedaNormalizada = this.normalizeText(busqueda[0]);
      prestamosFiltrados = prestamosFiltrados.filter(([_, prestamo]) => {
        switch (busqueda[1]) {
          case 'Usuario':
            return (
              this.normalizeText(
                prestamo.datosgrupo.NombreUsuario || '',
              ).includes(busquedaNormalizada) ||
              this.normalizeText(
                prestamo.datosgrupo.ApellidoPaternoUsuario || '',
              ).includes(busquedaNormalizada)
            );
          case 'Carnet':
            return this.normalizeText(
              prestamo.datosgrupo.CarnetUsuario || '',
            ).includes(busquedaNormalizada);
          case 'Teléfono':
            return this.normalizeText(
              prestamo.datosgrupo.TelefonoUsuario || '',
            ).includes(busquedaNormalizada);
          case 'Equipos':
            return this.normalizeText(
              prestamo.datosgrupo.NombreGrupoEquipo || '',
            ).includes(busquedaNormalizada);
          case 'Fecha Solicitud':
            return this.normalizeText(
              this.formatDate(prestamo.datosgrupo.FechaSolicitud),
            ).includes(busquedaNormalizada);
          case 'Fecha Préstamo Esperada':
            return this.normalizeText(
              this.formatDate(prestamo.datosgrupo.FechaPrestamoEsperada),
            ).includes(busquedaNormalizada);
          case 'Fecha Devolución Esperada':
            return this.normalizeText(
              this.formatDate(prestamo.datosgrupo.FechaDevolucionEsperada),
            ).includes(busquedaNormalizada);
          default:
            return (
              this.normalizeText(
                prestamo.datosgrupo.NombreUsuario || '',
              ).includes(busquedaNormalizada) ||
              this.normalizeText(
                prestamo.datosgrupo.ApellidoPaternoUsuario || '',
              ).includes(busquedaNormalizada) ||
              this.normalizeText(
                prestamo.datosgrupo.CarnetUsuario || '',
              ).includes(busquedaNormalizada) ||
              this.normalizeText(
                prestamo.datosgrupo.NombreGrupoEquipo || '',
              ).includes(busquedaNormalizada) ||
              this.normalizeText(prestamo.datosgrupo.CodigoImt || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(
                this.formatDate(prestamo.datosgrupo.FechaSolicitud),
              ).includes(busquedaNormalizada) ||
              this.normalizeText(
                this.formatDate(prestamo.datosgrupo.FechaPrestamoEsperada),
              ).includes(busquedaNormalizada) ||
              this.normalizeText(
                this.formatDate(prestamo.datosgrupo.FechaDevolucionEsperada),
              ).includes(busquedaNormalizada)
            );
        }
      });
    }
    if (this.estadoSeleccionado !== '') {
      const buscado = this.estadoSeleccionado.toLowerCase();
      prestamosFiltrados = prestamosFiltrados.filter(
        ([_, prestamo]) =>
          this.getEstadoCalculado(prestamo).toLowerCase() === buscado,
      );
    }
    if (this.rolSeleccionado !== '') {
      const rol = this.rolSeleccionado.toLowerCase();
      prestamosFiltrados = prestamosFiltrados.filter(
        ([_, prestamo]) =>
          prestamo.datosgrupo.TipoUsuario?.toLowerCase() === rol,
      );
    }
    if (this.fechaPrestamoDesde || this.fechaPrestamoHasta) {
      const desde = this.fechaPrestamoDesde
        ? new Date(`${this.fechaPrestamoDesde}T00:00:00`)
        : null;
      const hasta = this.fechaPrestamoHasta
        ? new Date(`${this.fechaPrestamoHasta}T23:59:59.999`)
        : null;
      prestamosFiltrados = prestamosFiltrados.filter(([, prestamo]) => {
        const fecha = prestamo.datosgrupo.FechaPrestamoEsperada;
        return (
          !!fecha && (!desde || fecha >= desde) && (!hasta || fecha <= hasta)
        );
      });
    }
    this.prestamos = new Map<number, PrestamoAgrupados>(prestamosFiltrados);
    this.reiniciarPaginacion();
    this.aplicarOrdenActualSiExiste();
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    const prestamosOrdenados = this.sortByColumn(
      Array.from(this.prestamos.entries()),
      e,
      {
        Usuario: ([, prestamo]) =>
          `${prestamo.datosgrupo.NombreUsuario ?? ''} ${prestamo.datosgrupo.ApellidoPaternoUsuario ?? ''}`,
        Carnet: ([, prestamo]) => prestamo.datosgrupo.CarnetUsuario,
        Rol: ([, prestamo]) => nombreRol(prestamo.datosgrupo.TipoUsuario),
        Teléfono: ([, prestamo]) => prestamo.datosgrupo.TelefonoUsuario,
        Equipos: ([, prestamo]) => prestamo.equipos[0]?.NombreGrupoEquipo,
        'Fecha Solicitud': ([, prestamo]) => prestamo.datosgrupo.FechaSolicitud,
        'Fecha Préstamo Esperada': ([, prestamo]) =>
          prestamo.datosgrupo.FechaPrestamoEsperada,
        'Fecha Devolución Esperada': ([, prestamo]) =>
          prestamo.datosgrupo.FechaDevolucionEsperada,
        Estado: ([, prestamo]) => this.getEstadoCalculado(prestamo),
      },
    );

    this.prestamos = new Map<number, PrestamoAgrupados>(prestamosOrdenados);
  }

  limpiarFiltros() {
    this.estadoSeleccionado = '';
    this.prestamos = new Map(this.prestamoscopia);
    this.fechaPrestamoDesde = '';
    this.fechaPrestamoHasta = '';
    this.busquedaActual = undefined;
    this.aplicarOrdenActualSiExiste();
  }
  getEstadoCalculado(prestamo: PrestamoAgrupados): string {
    const estadoOrig = (
      prestamo?.datosgrupo?.EstadoPrestamo || ''
    ).toLowerCase();
    if (!prestamo?.datosgrupo) return estadoOrig;
    const fechaDev = prestamo.datosgrupo.FechaDevolucionEsperada;
    if (!fechaDev) return estadoOrig;
    const ahora = new Date();
    if (
      (estadoOrig === 'activo' || estadoOrig === 'aprobado') &&
      fechaDev < ahora
    ) {
      return 'atrasado';
    }
    return estadoOrig;
  }
  validaraprobacion(key: number) {
    const prestamo = this.prestamos.get(key);
    if (!prestamo || !this.puedeAprobar(prestamo)) return;

    this.prestamoKeySeleccionado = key;
    this.detalleDecision = '';
    this.decisionPrestamo = 'aprobado';
  }

  puedeAprobar(prestamo: PrestamoAgrupados): boolean {
    const inicio = prestamo.datosgrupo.FechaPrestamoEsperada;

    return !!inicio && inicio.getTime() > Date.now();
  }
  aprobarprestamo(key: number) {
    if (this.procesandoDecision) return;
    this.procesandoDecision = true;
    this.prestamosapi
      .cambiarEstadoPrestamo(
        this.prestamos.get(key)!.datosgrupo.Id,
        'aprobado',
        this.detalleDecision.trim() || undefined,
      )
      .pipe(finalize(() => (this.procesandoDecision = false)))
      .subscribe({
        next: (_response) => {
          this.procesandoDecision = false;
          this.cerrarDecision();
          this.toast.success('Préstamo aprobado con éxito.');
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error);
          this.mensajeerror = `Error al aprobar el préstamo: ${errorMsg}`;
          this.error.set(true);
        },
      });
  }
  validarrechazo(key: number) {
    this.detalleDecision = '';
    this.prestamoKeySeleccionado = key;
    this.decisionPrestamo = 'rechazado';
  }
  rechazarprestamo(key: number) {
    if (!this.detalleDecision.trim() || this.procesandoDecision) return;
    this.procesandoDecision = true;
    this.prestamosapi
      .cambiarEstadoPrestamo(
        this.prestamos.get(key)!.datosgrupo.Id,
        'rechazado',
        this.detalleDecision.trim(),
      )
      .pipe(finalize(() => (this.procesandoDecision = false)))
      .subscribe({
        next: (_response) => {
          this.procesandoDecision = false;
          this.cerrarDecision();
          this.toast.success('Préstamo rechazado con éxito.');
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error);
          this.mensajeerror = `Error al rechazar el préstamo: ${errorMsg}`;
          this.error.set(true);
        },
      });
  }

  cerrarDecision(): void {
    if (this.procesandoDecision) return;
    this.decisionPrestamo = null;
    this.detalleDecision = '';
    this.prestamoKeySeleccionado = 0;
  }

  validarcancelacion(key: number) {
    this.mensajeavisocancelar =
      '¿Está seguro de cancelar el préstamo seleccionado?';
    this.prestamoKeySeleccionado = key;
    this.avisocancelar.set(true);
  }

  cancelarprestamo(key: number) {
    this.prestamosapi
      .cambiarEstadoPrestamo(
        this.prestamos.get(key)!.datosgrupo.Id,
        'cancelado',
      )
      .subscribe({
        next: (_response) => {
          this.toast.success('Préstamo cancelado con éxito.');
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error);
          this.mensajeerror = `Error al cancelar el préstamo: ${errorMsg}`;
          this.error.set(true);
        },
      });
    this.prestamoKeySeleccionado = 0;
    this.avisocancelar.set(false);
  }

  cambiarestadovercontrato(prestamo: PrestamoDto) {
    this.prestamoSeleccionado = prestamo;
    this.vercontrato.set(!this.vercontrato());
  }

  avisorecogido: WritableSignal<boolean> = signal(false);
  avisodevuelto: WritableSignal<boolean> = signal(false);
  observacionDevolucion: string = '';
  readonly estadoEquipoOpciones = ESTADO_EQUIPO_OPCIONES;

  estadosRetorno: { [codigoImt: string]: string } = {};
  equiposDevolucion: PrestamoDto[] = [];

  abrirVistaPrestamos(prestamos: PrestamoDto[]) {
    this.prestamosVista = prestamos;
    this.abrirVista = true;
  }
  cerrarVistaPrestamos() {
    this.abrirVista = false;
    this.prestamosVista = [];
  }

  validarRecogido(key: number) {
    this.prestamoKeySeleccionado = key;
    this.avisorecogido.set(true);
  }

  bloquearAlDevolver = false;

  validarDevuelto(key: number) {
    this.observacionDevolucion = '';
    this.bloquearAlDevolver = false;
    this.prestamoKeySeleccionado = key;
    this.equiposDevolucion = this.prestamos.get(key)?.equipos ?? [];
    this.estadosRetorno = {};
    for (const eq of this.equiposDevolucion) {
      if (eq.CodigoImt) this.estadosRetorno[eq.CodigoImt] = 'operativo';
    }
    this.avisodevuelto.set(true);
  }

  recogerprestamo(key: number) {
    this.prestamosapi
      .cambiarEstadoPrestamo(this.prestamos.get(key)!.datosgrupo.Id, 'activo')
      .subscribe({
        next: (_response) => {
          this.toast.success('Préstamo marcado como recogido con éxito.');
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error);
          this.mensajeerror = `Error al actualizar el préstamo: ${errorMsg}`;
          this.error.set(true);
        },
      });
    this.prestamoKeySeleccionado = 0;
    this.avisorecogido.set(false);
  }

  devolverprestamo(key: number) {
    const equiposRetorno = this.equiposDevolucion
      .filter((eq) => !!eq.CodigoImt)
      .map((eq) => ({
        CodigoImt: eq.CodigoImt!,
        EstadoEquipo: this.estadosRetorno[eq.CodigoImt!] ?? 'operativo',
      }));
    this.prestamosapi
      .cambiarEstadoPrestamo(
        this.prestamos.get(key)!.datosgrupo.Id,
        'finalizado',
        this.observacionDevolucion,
        equiposRetorno,
      )
      .subscribe({
        next: (_response) => {
          this.toast.success('Préstamo marcado como devuelto con éxito.');
          this.auditRefresh++;
          if (this.bloquearAlDevolver) {
            this.bloquearUsuarioDevolucion(
              this.prestamos.get(key)!.datosgrupo.CarnetUsuario || '',
            );
          }
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error);
          this.mensajeerror = `Error al devolver el préstamo: ${errorMsg}`;
          this.error.set(true);
        },
      });
    this.prestamoKeySeleccionado = 0;
    this.avisodevuelto.set(false);
  }

  private bloquearUsuarioDevolucion(carnet: string) {
    if (!carnet) return;

    this.usuarioapi
      .bloquearUsuario(carnet, true, this.observacionDevolucion || null)
      .subscribe({ next: () => {}, error: () => {} });
  }
}
