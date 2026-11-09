import { INVENTORY_LABELS } from '@shared/config';
import { CommonModule, DatePipe } from '@angular/common';
import {
  Component,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditLogDto } from '../../model/audit-log';
import { AuditLogApiService } from '../../api/audit-log-api.service';
import {
  estadoEquipoCssClass,
  estadoEquipoLabel,
} from '@entities/equipment/@x/audit-log';
import {
  FlatpickrDirective,
  StickyScrollDirective,
} from '@shared/lib/directives';
import {
  AriaOrden,
  ariaOrden,
  ColumnaOrdenableComponent,
  iconoOrden,
  printTable,
  siguienteDireccion,
  TablePaginationComponent,
} from '@shared/lib/admin-table';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import { formatRegionDateTime } from '@shared/lib/date';
import { AuditObservationDetail } from '../../model/audit-observation-detail';
import {
  parseAuditDetail,
  separarPalabras,
} from '../../model/audit-detail-parser';
import {
  ACCIONES_POR_ENTIDAD,
  badgeAccion,
  descripcionAccion,
  etiquetaEntidad,
} from '../../model/audit-labels';

const CAMPO_CODIGO_INTERNO = 'Código interno';
const INTERVALO_ACTUALIZACION_MS = 30000;

@Component({
  selector: 'app-audit-panel',
  standalone: true,
  imports: [
    ColumnaOrdenableComponent,
    CommonModule,
    DatePipe,
    FormsModule,
    FlatpickrDirective,
    StickyScrollDirective,
    TablePaginationComponent,
    CustomSelectComponent,
  ],
  templateUrl: './audit-panel.component.html',
  styleUrl: './audit-panel.component.css',
})
export class AuditPanelComponent implements OnChanges, OnDestroy {
  protected readonly etiquetas = INVENTORY_LABELS;
  @Input() entidad!: string;
  @Input() refreshTrigger: number = 0;

  logs: AuditLogDto[] = [];
  readonly columnas = ['Fecha', 'Actor', 'Acción', 'Registro', 'Acciones'];
  sortColumn = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  cargando = true;
  fechaDesde = '';
  fechaHasta = '';
  filtroAccion = '';
  filtroAdmin = '';
  paginaActual = 1;
  readonly filasPorPagina = 10;
  opcionesAccion: OpcionSelect[] = [];
  logsPaginados: AuditLogDto[] = [];
  private readonly detalleCache = new Map<
    string,
    AuditObservationDetail | null
  >();
  private refreshTimer?: ReturnType<typeof setInterval>;

  get acciones(): string[] {
    return (
      ACCIONES_POR_ENTIDAD[this.entidad] ?? ['Crear', 'Editar', 'Eliminar']
    );
  }
  constructor(private readonly auditService: AuditLogApiService) {}

  ngOnChanges(changes: SimpleChanges) {
    if (changes['entidad']) this.actualizarOpcionesAccion();
    if (changes['entidad'] || changes['refreshTrigger']) this.cargar();
    this.iniciarActualizacionAutomatica();
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  onFechaDesde(dates: Date[]) {
    this.fechaDesde = dates[0] ? this.inicioDelDia(dates[0]).toISOString() : '';
    this.cargar();
  }

  onFechaHasta(dates: Date[]) {
    this.fechaHasta = dates[0] ? this.finDelDia(dates[0]).toISOString() : '';
    this.cargar();
  }

  cargar(silencioso = false) {
    if (!silencioso) this.cargando = true;
    const paginaDestino = silencioso ? this.paginaActual : 1;
    this.auditService
      .getAuditLog(
        this.entidad,
        this.filtroAdmin || undefined,
        this.filtroAccion || undefined,
        this.fechaDesde || undefined,
        this.fechaHasta || undefined,
      )
      .subscribe({
        next: (data) => {
          this.logs = data;
          this.detalleCache.clear();
          this.aplicarOrdenActual();
          const ultimaPagina = Math.max(
            1,
            Math.ceil(this.logs.length / this.filasPorPagina),
          );
          this.cambiarPagina(Math.min(paginaDestino, ultimaPagina));
          this.cargando = false;
        },
        error: () => {
          this.cargando = false;
        },
      });
  }

  private iniciarActualizacionAutomatica(): void {
    if (this.refreshTimer) return;

    this.refreshTimer = setInterval(() => {
      if (!this.cargando && !this.obsAbierta && !document.hidden)
        this.cargar(true);
    }, INTERVALO_ACTUALIZACION_MS);
  }

  seleccionarAccion(a: string) {
    this.filtroAccion = a;
    this.cargar();
  }

  cambiarPagina(pagina: number): void {
    this.paginaActual = pagina;
    const inicio = (pagina - 1) * this.filasPorPagina;
    this.logsPaginados = this.logs.slice(inicio, inicio + this.filasPorPagina);
  }

  limpiarFiltroAdmin() {
    this.filtroAdmin = '';
    this.cargar();
  }

  exportarCsv(): void {
    const rows = this.logs.map((log) => [
      log.Timestamp instanceof Date
        ? log.Timestamp.toISOString()
        : log.Timestamp,
      log.AdminNombre || log.AdminCarnet,
      log.Accion,
      this.registroLabel(log),
      this.resumenObs(log),
    ]);
    const csv = [['Fecha', 'Actor', 'Acción', 'Registro', 'Detalle'], ...rows]
      .map((row) =>
        row
          .map((value) => `"${String(value ?? '').replaceAll('"', '""')}"`)
          .join(','),
      )
      .join('\n');
    const link = document.createElement('a');
    link.href = URL.createObjectURL(
      new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8' }),
    );
    link.download = `auditoria-${this.entidad.toLowerCase()}.csv`;
    link.click();
    URL.revokeObjectURL(link.href);
  }

  imprimir(): void {
    printTable({
      title: `Auditoría: ${this.entidad}`,
      headers: ['Fecha', 'Actor', 'Acción', 'Registro', 'Detalle'],
      rows: this.logs.map((log) => [
        formatRegionDateTime(log.Timestamp),
        log.AdminNombre || log.AdminCarnet,
        log.Accion,
        this.registroLabel(log),
        this.resumenObs(log),
      ]),
    });
  }

  ordenarPorColumna(columna: string): void {
    const columnaOrdenable = columna.trim();

    if (!this.esColumnaOrdenable(columnaOrdenable)) return;

    this.sortDirection = siguienteDireccion(
      this.sortColumn,
      this.sortDirection,
      columnaOrdenable,
    );
    this.sortColumn = columnaOrdenable;
    this.aplicarOrdenActual();
    this.cambiarPagina(1);
  }

  esColumnaOrdenada(columna: string): boolean {
    return this.sortColumn === columna.trim();
  }

  esColumnaOrdenable(columna: string): boolean {
    return columna.trim() !== 'Acciones';
  }

  ariaOrdenColumna(columna: string): AriaOrden {
    return ariaOrden(this.esColumnaOrdenada(columna), this.sortDirection);
  }

  iconoOrdenColumna(columna: string): string {
    return iconoOrden(this.esColumnaOrdenada(columna), this.sortDirection);
  }

  obsAbierta: AuditObservationDetail | null = null;

  parseDetalle(detalle?: string): AuditObservationDetail | null {
    if (!detalle) return null;
    const cached = this.detalleCache.get(detalle);
    if (cached) return cached;

    const resultado = parseAuditDetail(detalle);
    this.detalleCache.set(detalle, resultado);
    return resultado;
  }

  resumenObs(log: AuditLogDto): string {
    const p = this.parseDetalle(log.Detalle);
    if (!p) return this.descripcionAccion(log);
    return (
      p.observacion ||
      p.texto ||
      (p.equiposPrestamo ? `Reserva de ${p.equiposPrestamo}` : undefined) ||
      (p.cambios?.length
        ? `${p.cambios.length} campo${p.cambios.length === 1 ? '' : 's'} modificado${p.cambios.length === 1 ? '' : 's'}`
        : undefined) ||
      (p.equipos?.length ? 'Estados de equipos registrados' : undefined) ||
      (p.datos?.length ? 'Información de la acción registrada' : undefined) ||
      this.descripcionAccion(log)
    );
  }

  obsLogAbierto: AuditLogDto | null = null;

  abrirObs(log: AuditLogDto): void {
    this.obsAbierta = this.parseDetalle(log.Detalle) ?? {
      texto: this.mensajeDetalleHistorico(log),
    };
    this.obsLogAbierto = log;
  }

  detenerPropagacion(event: Event): void {
    event.stopPropagation();
  }

  cerrarObs(): void {
    this.obsAbierta = null;
    this.obsLogAbierto = null;
  }

  formatearFechaDetalle(fecha?: string | Date): string {
    if (!fecha) return '—';

    return (
      formatRegionDateTime(fecha) ||
      (fecha instanceof Date ? fecha.toISOString() : fecha)
    );
  }

  protected readonly estadoEquipoLabel = estadoEquipoLabel;
  protected readonly estadoEquipoCssClass = estadoEquipoCssClass;

  badgeClass(accion: string | undefined): string {
    return badgeAccion(accion);
  }

  accionLabel(accion?: string): string {
    return accion ? separarPalabras(accion) : 'Acción registrada';
  }

  entidadLabel(entidad?: string): string {
    return etiquetaEntidad(entidad, this.entidad);
  }

  registroLabel(log: AuditLogDto): string {
    if (log.EntidadNombre?.trim()) return log.EntidadNombre.trim();

    return this.nombreDesdeDetalle(log) || this.entidadLabel(log.Entidad);
  }

  descripcionAccion(log: AuditLogDto): string {
    return descripcionAccion(
      log.Accion,
      this.entidadLabel(log.Entidad).toLowerCase(),
      this.registroLabel(log),
    );
  }

  private auditSortValue(log: AuditLogDto, columna: string): unknown {
    const values: Record<string, unknown> = {
      Fecha: log.Timestamp,
      Actor: log.AdminNombre || log.AdminCarnet,
      Acción: log.Accion,
      Registro: this.registroLabel(log),
    };

    return values[columna];
  }

  private aplicarOrdenActual(): void {
    if (!this.sortColumn) return;

    this.logs = [...this.logs].sort((a, b) =>
      this.compareAuditValues(
        this.auditSortValue(a, this.sortColumn),
        this.auditSortValue(b, this.sortColumn),
      ),
    );
  }

  private actualizarOpcionesAccion(): void {
    this.opcionesAccion = [
      { value: '', label: 'Todas las acciones' },
      ...this.acciones.map((accion) => ({ value: accion, label: accion })),
    ];
  }

  private nombreDesdeDetalle(log: AuditLogDto): string {
    const detail = this.parseDetalle(log.Detalle);
    if (!detail) return '';

    if (detail.usuarioNombre?.trim()) {
      return log.Entidad === 'Prestamo'
        ? `Préstamo de ${detail.usuarioNombre.trim()}`
        : detail.usuarioNombre.trim();
    }

    const changes = detail.cambios ?? [];
    const valueFor = (field: string) => {
      const change = changes.find(
        (item) =>
          item.campo?.localeCompare(field, undefined, {
            sensitivity: 'base',
          }) === 0,
      );
      return (change?.nuevo || change?.anterior || '').trim();
    };

    if (log.Entidad === 'Usuario') {
      return [
        valueFor('Nombre'),
        valueFor('Apellido paterno'),
        valueFor('Apellido materno'),
      ]
        .filter(Boolean)
        .join(' ');
    }

    if (log.Entidad === 'Equipo') {
      const code = valueFor(CAMPO_CODIGO_INTERNO);
      const name = valueFor('Nombre') || valueFor('Grupo de equipos');
      if (code && name)
        return `${INVENTORY_LABELS.internalCodeOf(code)} · ${name}`;
      if (code) return INVENTORY_LABELS.internalCodeOf(code);
    }

    return valueFor('Nombre');
  }

  private mensajeDetalleHistorico(log: AuditLogDto): string {
    if (log.Accion?.toLowerCase() === 'editar') {
      return 'Este registro histórico no conserva los valores anteriores y posteriores de la edición.';
    }

    return this.descripcionAccion(log);
  }

  private inicioDelDia(fecha: Date): Date {
    const inicio = new Date(fecha);
    inicio.setHours(0, 0, 0, 0);
    return inicio;
  }

  private finDelDia(fecha: Date): Date {
    const fin = new Date(fecha);
    fin.setHours(23, 59, 59, 999);
    return fin;
  }

  private compareAuditValues(
    firstValue: unknown,
    secondValue: unknown,
  ): number {
    const firstDate = firstValue instanceof Date ? firstValue.getTime() : NaN;
    const secondDate =
      secondValue instanceof Date ? secondValue.getTime() : NaN;

    const result =
      Number.isFinite(firstDate) && Number.isFinite(secondDate)
        ? firstDate - secondDate
        : String(firstValue ?? '').localeCompare(
            String(secondValue ?? ''),
            undefined,
            { numeric: true, sensitivity: 'base' },
          );

    return this.sortDirection === 'asc' ? result : -result;
  }
}
