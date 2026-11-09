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
  ROL_ADMINISTRADOR,
} from '@entities/user';
import {
  Aviso,
  AvisoEliminarComponent,
  BuscadorComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  PantallaCargaComponent,
  ToastService,
} from '@shared/ui';
import {
  AdminTabsComponent,
  ColumnaOrdenableComponent,
  Tabla,
  TablePaginationComponent,
  printTable,
} from '@shared/lib/admin-table';
import {
  FlatpickrDirective,
  StickyScrollDirective,
} from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import { AuditPanelComponent } from '@entities/audit-log';
import { finalize, Observable } from 'rxjs';
import {
  COLUMNAS_PRESTAMOS,
  ORDEN_PRESTAMOS,
  ROLES_FILTRO_PRESTAMOS,
  camposBusqueda,
  detalleEquipos,
  enRangoDeFechas,
  estadoCalculado,
  filaExportacion,
  resumenEquipo,
} from '../../model/filtros-prestamos';
import {
  DevolucionPrestamo,
  DevolucionPrestamoComponent,
} from '../devolucion-prestamo/devolucion-prestamo.component';
import {
  DecisionPrestamo,
  DecisionPrestamoComponent,
} from '../decision-prestamo/decision-prestamo.component';

@Component({
  selector: 'app-prestamos-tabla',
  standalone: true,
  imports: [
    ColumnaOrdenableComponent,
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
    DevolucionPrestamoComponent,
    DecisionPrestamoComponent,
  ],
  templateUrl: './prestamos-tabla.component.html',
  styleUrls: [
    '../../../../shared/lib/admin-table/tabla-admin.css',
    '../modal-prestamo.css',
    './prestamos-tabla.component.css',
  ],
})
export class PrestamosTablaComponent extends Tabla implements OnInit {
  private readonly toast = inject(ToastService);
  private readonly prestamosapi = inject(PrestamosAPIService);
  private readonly usuarioapi = inject(UsuarioServiceAPI);
  readonly esRoot =
    inject(UsuarioService).obtenerUsuario().rol?.toLowerCase() ===
    ROL_ADMINISTRADOR;
  expandedRowId: number | null = null;
  auditRefresh = 0;
  cargando = false;
  alertaeliminar = false;
  prestamos = new Map<number, PrestamoAgrupados>();
  prestamoscopia = new Map<number, PrestamoAgrupados>();
  vercontrato: WritableSignal<boolean> = signal(false);
  prestamoSeleccionado: PrestamoDto = new PrestamoDto();
  prestamoKeySeleccionado = 0;
  decisionPrestamo: DecisionPrestamo | null = null;
  avisocancelar: WritableSignal<boolean> = signal(false);
  avisorecogido: WritableSignal<boolean> = signal(false);
  avisodevuelto: WritableSignal<boolean> = signal(false);
  mensajeavisocancelar = '¿Está seguro de cancelar el préstamo seleccionado?';
  override columnas: string[] = [...COLUMNAS_PRESTAMOS];
  estadoSeleccionado = '';
  readonly estadoFiltroOpciones = ESTADO_PRESTAMO_FILTRO_OPCIONES;
  rolSeleccionado = '';
  readonly rolesFiltroOpciones = ROLES_FILTRO_PRESTAMOS;
  fechaPrestamoDesde = '';
  fechaPrestamoHasta = '';
  abrirVista = false;
  prestamosVista: PrestamoDto[] = [];
  observacionEditar = '';
  editandoObservacion: number | null = null;
  equiposDevolucion: PrestamoDto[] = [];
  private busquedaActual?: [string, string];

  get prestamosTabla() {
    return Array.from(this.prestamos.entries(), ([key, value]) => ({
      key,
      value,
    }));
  }

  private get columnasExportacion(): string[] {
    return [...this.columnas, 'Estado'];
  }

  ngOnInit() {
    this.cargarPrestamos();
  }

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }

  resumenEquipo(prestamo: PrestamoAgrupados): string {
    return resumenEquipo(prestamo);
  }

  detalleEquipos(prestamo: PrestamoAgrupados): string {
    return detalleEquipos(prestamo);
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
        error: (e) => this.mostrarError(extractErrorMessage(e)),
      });
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
      error: (error) =>
        this.mostrarError(
          extractErrorMessage(
            error,
            'Error al cargar los préstamos. Por favor, inténtelo de nuevo más tarde.',
          ),
        ),
    });
  }

  agruparPrestamos(datos: PrestamoDto[]) {
    this.prestamos.clear();
    for (const prestamo of [...datos].sort(compararPrestamos)) {
      if (prestamo.Id == null) continue;
      const grupo = this.prestamos.get(prestamo.Id);
      if (grupo) grupo.insertarEquipo(prestamo);
      else this.prestamos.set(prestamo.Id, new PrestamoAgrupados([prestamo]));
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
      .subscribe(
        this.tras('Préstamo eliminado con éxito.', (error) =>
          extractErrorMessage(
            error,
            'Error al eliminar el préstamo. Por favor, inténtelo de nuevo más tarde.',
          ),
        ),
      );
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
    const filtrados = Array.from(this.prestamoscopia.entries()).filter(
      ([, prestamo]) => this.cumpleFiltros(prestamo),
    );
    this.prestamos = new Map<number, PrestamoAgrupados>(filtrados);
    this.reiniciarPaginacion();
    this.aplicarOrdenActualSiExiste();
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.prestamos = new Map<number, PrestamoAgrupados>(
      this.sortByColumn(
        Array.from(this.prestamos.entries()),
        e,
        ORDEN_PRESTAMOS,
      ),
    );
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
    return estadoCalculado(prestamo);
  }

  puedeAprobar(prestamo: PrestamoAgrupados): boolean {
    const inicio = prestamo.datosgrupo.FechaPrestamoEsperada;

    return !!inicio && inicio.getTime() > Date.now();
  }

  validaraprobacion(key: number) {
    const prestamo = this.prestamos.get(key);
    if (!prestamo || !this.puedeAprobar(prestamo)) return;

    this.abrirDecision(key, 'aprobado');
  }

  validarrechazo(key: number) {
    this.abrirDecision(key, 'rechazado');
  }

  cerrarDecision(): void {
    this.decisionPrestamo = null;
    this.prestamoKeySeleccionado = 0;
  }

  alDecidir(mensaje: string): void {
    this.cerrarDecision();
    this.alExito(mensaje);
  }

  validarcancelacion(key: number) {
    this.mensajeavisocancelar =
      '¿Está seguro de cancelar el préstamo seleccionado?';
    this.prestamoKeySeleccionado = key;
    this.avisocancelar.set(true);
  }

  cancelarprestamo(key: number) {
    this.cambiarEstado(key, 'cancelado').subscribe(
      this.tras(
        'Préstamo cancelado con éxito.',
        (error) =>
          `Error al cancelar el préstamo: ${extractErrorMessage(error)}`,
      ),
    );
    this.prestamoKeySeleccionado = 0;
    this.avisocancelar.set(false);
  }

  cambiarestadovercontrato(prestamo: PrestamoDto) {
    this.prestamoSeleccionado = prestamo;
    this.vercontrato.set(!this.vercontrato());
  }

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

  validarDevuelto(key: number) {
    this.prestamoKeySeleccionado = key;
    this.equiposDevolucion = this.prestamos.get(key)?.equipos ?? [];
    this.avisodevuelto.set(true);
  }

  recogerprestamo(key: number) {
    this.cambiarEstado(key, 'activo').subscribe(
      this.tras(
        'Préstamo marcado como recogido con éxito.',
        (error) =>
          `Error al actualizar el préstamo: ${extractErrorMessage(error)}`,
      ),
    );
    this.prestamoKeySeleccionado = 0;
    this.avisorecogido.set(false);
  }

  devolverprestamo(key: number, devolucion: DevolucionPrestamo) {
    const carnet = this.prestamos.get(key)!.datosgrupo.CarnetUsuario || '';
    this.cambiarEstado(
      key,
      'finalizado',
      devolucion.observacion,
      devolucion.equipos,
    ).subscribe(
      this.tras(
        'Préstamo marcado como devuelto con éxito.',
        (error) =>
          `Error al devolver el préstamo: ${extractErrorMessage(error)}`,
        () => {
          if (devolucion.bloquearUsuario)
            this.bloquearUsuario(carnet, devolucion.observacion);
        },
      ),
    );
    this.prestamoKeySeleccionado = 0;
    this.avisodevuelto.set(false);
  }

  private cumpleFiltros(prestamo: PrestamoAgrupados): boolean {
    const busqueda = this.busquedaActual;
    const texto = busqueda?.[0].trim() ? this.normalizeText(busqueda[0]) : '';
    const coincide =
      !texto ||
      camposBusqueda(prestamo, busqueda![1], (fecha) =>
        this.formatDate(fecha),
      ).some((valor) => this.normalizeText(valor).includes(texto));

    return (
      coincide &&
      (this.estadoSeleccionado === '' ||
        this.getEstadoCalculado(prestamo).toLowerCase() ===
          this.estadoSeleccionado.toLowerCase()) &&
      (this.rolSeleccionado === '' ||
        prestamo.datosgrupo.TipoUsuario?.toLowerCase() ===
          this.rolSeleccionado.toLowerCase()) &&
      enRangoDeFechas(
        prestamo,
        this.fechaPrestamoDesde,
        this.fechaPrestamoHasta,
      )
    );
  }

  private filasExportacion() {
    return this.prestamosTabla.map(({ value }) => filaExportacion(value));
  }

  private abrirDecision(key: number, decision: DecisionPrestamo) {
    this.prestamoKeySeleccionado = key;
    this.decisionPrestamo = decision;
  }

  private cambiarEstado(
    key: number,
    ...cambio: [
      estado: string,
      observacion?: string,
      equipos?: DevolucionPrestamo['equipos'],
    ]
  ): Observable<unknown> {
    return this.prestamosapi.cambiarEstadoPrestamo(
      this.prestamos.get(key)!.datosgrupo.Id,
      ...cambio,
    );
  }

  private tras(
    mensajeExito: string,
    mensajeError: (error: unknown) => string,
    antes?: () => void,
  ) {
    return {
      next: () => this.alExito(mensajeExito, antes),
      error: (error: unknown) => this.mostrarError(mensajeError(error)),
    };
  }

  private alExito(mensaje: string, antes?: () => void) {
    antes?.();
    this.toast.success(mensaje);
    this.auditRefresh++;
    this.cargarPrestamos();
  }

  mostrarError(mensaje: string) {
    this.mensajeerror = mensaje;
    this.error.set(true);
  }

  private bloquearUsuario(carnet: string, motivo: string) {
    if (!carnet) return;

    this.usuarioapi
      .bloquearUsuario(carnet, true, motivo || null)
      .subscribe({ next: () => {}, error: () => {} });
  }
}
