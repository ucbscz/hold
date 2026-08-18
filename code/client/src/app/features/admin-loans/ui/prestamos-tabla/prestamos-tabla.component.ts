import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { PrestamoDto } from '@entities/admin';
import {
  PrestamoAgrupados,
  PrestamosAPIService,
  VistaPrestamosComponent,
} from '@entities/loan';
import { UsuarioServiceAPI } from '@entities/user';
import { BuscadorComponent } from '@features/admin-search';
import {
  AdminTableSort,
  Tabla,
  TablePaginationComponent,
} from '@shared/lib/admin-table';
import {
  FlatpickrDirective,
  StickyScrollDirective,
} from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoEliminarComponent,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
  PantallaCargaComponent,
} from '@shared/ui';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { finalize } from 'rxjs';
import { VercontratoComponent } from '../vercontrato/vercontrato.component';
@Component({
  selector: 'app-prestamos-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    VercontratoComponent,
    PantallaCargaComponent,
    VistaPrestamosComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    BuscadorComponent,
    AuditPanelComponent,
    CustomSelectComponent,
    TablePaginationComponent,
    FlatpickrDirective,
  ],
  templateUrl: './prestamos-tabla.component.html',
  styleUrls: ['./prestamos-tabla.component.css'],
})
export class PrestamosTablaComponent extends Tabla implements OnInit {
  override sortColumn = 'Fecha Solicitud';
  override sortDirection: AdminTableSort['dir'] = 'desc';
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }
  botoncrear: WritableSignal<boolean> = signal(false);
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
      ['ID', 'Estudiante', 'Carnet', 'Equipos', 'Inicio', 'Fin', 'Estado'],
      this.prestamosTabla.map(({ key, value }) => [
        key,
        `${value.datosgrupo.NombreUsuario ?? ''} ${value.datosgrupo.ApellidoPaternoUsuario ?? ''}`.trim(),
        value.datosgrupo.CarnetUsuario,
        this.detalleEquipos(value),
        value.datosgrupo.FechaPrestamoEsperada?.toISOString() ?? '',
        value.datosgrupo.FechaDevolucionEsperada?.toISOString() ?? '',
        this.getEstadoCalculado(value),
      ]),
    );
  }

  imprimirPrestamos(): void {
    const tabla = document.querySelector('.data-table--loans');
    if (!tabla) return;
    const ventana = window.open('', '_blank');
    if (!ventana) return;
    const clon = tabla.cloneNode(true) as HTMLTableElement;
    clon
      .querySelectorAll('.actions-column, .loan-actions')
      .forEach((node) => node.remove());
    ventana.document.write(
      `<!doctype html><html><head><title>Préstamos</title><style>body{font-family:Arial,sans-serif;padding:24px;color:#172033}h1{font-size:20px}table{width:100%;border-collapse:collapse}th,td{padding:9px;border:1px solid #d7dee8;text-align:left;font-size:12px}th{background:#f5f7fa}</style></head><body><h1>Préstamos</h1>${clon.outerHTML}</body></html>`,
    );
    ventana.document.close();
    ventana.focus();
    setTimeout(() => ventana.print(), 100);
    ventana.onafterprint = () => ventana.close();
  }

  vercontrato: WritableSignal<boolean> = signal(false);
  prestamoSeleccionado: PrestamoDto = new PrestamoDto();
  prestamoKeySeleccionado: number = 0;
  avisorechazar: WritableSignal<boolean> = signal(false);
  mensajeavisorechazar: string =
    '¿Está seguro de rechazar el préstamo seleccionado?';
  avisocancelar: WritableSignal<boolean> = signal(false);
  mensajeavisocancelar: string =
    '¿Está seguro de cancelar el préstamo seleccionado?';
  override columnas: string[] = [
    'Usuario',
    'Carnet',
    'Teléfono',
    'Equipos',
    'Fecha Solicitud',
    'Fecha Préstamo Esperada',
    'Fecha Devolución Esperada',
  ];
  showEstados: boolean = false;
  estadoSeleccionado: string = '';
  estadosDisponibles: string[] = [
    'atrasado',
    'pendiente',
    'rechazado',
    'aprobado',
    'activo',
    'finalizado',
    'cancelado',
  ];
  fechaPrestamoDesde = '';
  fechaPrestamoHasta = '';
  private busquedaActual?: [string, string];
  abrirVista: boolean = false;
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
  crearprestamo() {
    this.botoncrear.set(true);
  }
  cargarPrestamos() {
    this.prestamosapi.obtenerPrestamos().subscribe({
      next: (data: PrestamoDto[]) => {
        data.sort(
          (a, b) =>
            new Date(b.FechaSolicitud ?? 0).getTime() -
            new Date(a.FechaSolicitud ?? 0).getTime(),
        );
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
    for (const prestamo of datos) {
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
          this.mensajeexito = 'Préstamo eliminado con éxito.';
          this.exito.set(true);
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
  mostrarEstados() {
    this.showEstados = !this.showEstados;
  }
  seleccionarEstado(estado: string) {
    this.estadoSeleccionado = estado;
    this.showEstados = false;
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
        Teléfono: ([, prestamo]) => prestamo.datosgrupo.TelefonoUsuario,
        Equipos: ([, prestamo]) => prestamo.datosgrupo.NombreGrupoEquipo,
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
    this.showEstados = false;
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
    this.mensajeaviso = '¿Está seguro de aprobar el préstamo seleccionado?';
    this.prestamoKeySeleccionado = key;
    this.aviso.set(true);
  }
  aprobarprestamo(key: number) {
    this.prestamosapi
      .cambiarEstadoPrestamo(this.prestamos.get(key)!.datosgrupo.Id, 'aprobado')
      .subscribe({
        next: (_response) => {
          this.mensajeexito = 'Préstamo aprobado con éxito.';
          this.exito.set(true);
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error);
          this.mensajeerror = `Error al aprobar el préstamo: ${errorMsg}`;
          this.error.set(true);
        },
      });
    this.prestamoKeySeleccionado = 0;
  }
  validarrechazo(key: number) {
    this.mensajeavisorechazar =
      '¿Está seguro de rechazar el préstamo seleccionado?';
    this.prestamoKeySeleccionado = key;
    this.avisorechazar.set(true);
  }
  rechazarprestamo(key: number) {
    this.prestamosapi
      .cambiarEstadoPrestamo(
        this.prestamos.get(key)!.datosgrupo.Id,
        'rechazado',
      )
      .subscribe({
        next: (_response) => {
          this.mensajeexito = 'Préstamo rechazado con éxito.';
          this.exito.set(true);
          this.auditRefresh++;
          this.cargarPrestamos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(error);
          this.mensajeerror = `Error al rechazar el préstamo: ${errorMsg}`;
          this.error.set(true);
        },
      });
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
          this.mensajeexito = 'Préstamo cancelado con éxito.';
          this.exito.set(true);
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
  estadoEquipoOpciones: OpcionSelect[] = [
    { value: 'operativo', label: 'Operativo' },
    { value: 'parcialmente_operativo', label: 'Parcialmente operativo' },
    { value: 'inoperativo', label: 'Inoperativo' },
  ];

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
          this.mensajeexito = 'Préstamo marcado como recogido con éxito.';
          this.exito.set(true);
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
          this.mensajeexito = 'Préstamo marcado como devuelto con éxito.';
          this.exito.set(true);
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
