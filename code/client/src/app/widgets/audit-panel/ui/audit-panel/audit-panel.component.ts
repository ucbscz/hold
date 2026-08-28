import { CommonModule, DatePipe } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditLogDto } from '@entities/admin';
import { AuditLogApiService } from '@entities/audit-log';
import { FlatpickrDirective } from '@shared/lib/directives';
import { printTable, TablePaginationComponent } from '@shared/lib/admin-table';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import { parseJsonResult } from '@shared/lib/result';
import { AuditObservationDetail } from '../../model/audit-observation-detail';

const ACCIONES_POR_ENTIDAD: Record<string, string[]> = {
  Prestamo: [
    'Crear',
    'Aprobar',
    'Rechazar',
    'Recoger',
    'Devolver',
    'Cancelar',
    'AtrasadoAutomatico',
    'RegistrarContrato',
    'EliminarContrato',
    'Eliminar',
  ],
  Usuario: ['Crear', 'Editar', 'Bloquear', 'Desbloquear', 'Eliminar'],
  Equipo: ['Crear', 'Editar', 'Eliminar'],
  GrupoEquipo: ['Crear', 'Editar', 'EliminarComentario', 'Eliminar'],
  Accesorio: ['Crear', 'Editar', 'Eliminar'],
  Componente: ['Crear', 'Editar', 'Eliminar'],
  Gavetero: ['Crear', 'Editar', 'Eliminar'],
  Mueble: ['Crear', 'Editar', 'Eliminar'],
  Mantenimiento: ['Crear', 'Editar', 'Eliminar'],
  EmpresaMantenimiento: ['Crear', 'Editar', 'Eliminar'],
  Carrera: ['Crear', 'Editar', 'Eliminar'],
  Categoria: ['Crear', 'Editar', 'Eliminar'],
};

@Component({
  selector: 'app-audit-panel',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    FlatpickrDirective,
    TablePaginationComponent,
    CustomSelectComponent,
  ],
  templateUrl: './audit-panel.component.html',
  styleUrl: './audit-panel.component.css',
})
export class AuditPanelComponent implements OnChanges {
  @Input() entidad!: string;
  @Input() refreshTrigger: number = 0;

  logs: AuditLogDto[] = [];
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

  get acciones(): string[] {
    return (
      ACCIONES_POR_ENTIDAD[this.entidad] ?? ['Crear', 'Editar', 'Eliminar']
    );
  }
  constructor(private readonly auditService: AuditLogApiService) {}

  ngOnChanges(changes: SimpleChanges) {
    if (changes['entidad']) this.actualizarOpcionesAccion();
    if (changes['entidad'] || changes['refreshTrigger']) this.cargar();
  }

  onFechaDesde(dates: Date[]) {
    this.fechaDesde = dates[0] ? this.inicioDelDia(dates[0]).toISOString() : '';
    this.cargar();
  }

  onFechaHasta(dates: Date[]) {
    this.fechaHasta = dates[0] ? this.finDelDia(dates[0]).toISOString() : '';
    this.cargar();
  }

  cargar() {
    this.cargando = true;
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
          this.cambiarPagina(1);
          this.cargando = false;
        },
        error: () => {
          this.cargando = false;
        },
      });
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
      log.EntidadId,
      this.resumenObs(log),
    ]);
    const csv = [['Fecha', 'Actor', 'Acción', 'ID', 'Detalle'], ...rows]
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
      headers: ['Fecha', 'Actor', 'Acción', 'ID', 'Detalle'],
      rows: this.logs.map((log) => [
        this.formatearFechaImpresion(log.Timestamp),
        log.AdminNombre || log.AdminCarnet,
        log.Accion,
        log.EntidadId,
        this.resumenObs(log),
      ]),
    });
  }

  private formatearFechaImpresion(
    fecha: Date | string | null | undefined,
  ): string {
    if (!fecha) return '';
    const valor = fecha instanceof Date ? fecha : new Date(fecha);
    if (Number.isNaN(valor.getTime())) return '';

    return new Intl.DateTimeFormat('es-BO', {
      timeZone: 'America/La_Paz',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    }).format(valor);
  }

  ordenarPorColumna(columna: string): void {
    const columnaOrdenable = columna.trim();

    if (!columnaOrdenable) return;

    this.sortDirection =
      this.sortColumn === columnaOrdenable && this.sortDirection === 'asc'
        ? 'desc'
        : 'asc';
    this.sortColumn = columnaOrdenable;
    this.aplicarOrdenActual();
    this.cambiarPagina(this.paginaActual);
  }

  esColumnaOrdenada(columna: string): boolean {
    return this.sortColumn === columna.trim();
  }

  iconoOrdenColumna(columna: string): string {
    if (!this.esColumnaOrdenada(columna)) return 'fa-sort';

    return this.sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
  }

  obsAbierta: AuditObservationDetail | null = null;

  parseDetalle(detalle?: string): AuditObservationDetail | null {
    if (!detalle) return null;
    if (this.detalleCache.has(detalle)) {
      return this.detalleCache.get(detalle) ?? null;
    }

    const parsedDetail = parseJsonResult<unknown>(detalle);

    if (
      parsedDetail.isOk() &&
      this.isAuditObservationDetail(parsedDetail.value)
    ) {
      const structuredDetail = parsedDetail.value.texto
        ? (this.parseLegacyLoanDetail(parsedDetail.value.texto) ??
          parsedDetail.value)
        : parsedDetail.value;
      this.detalleCache.set(detalle, structuredDetail);
      return structuredDetail;
    }

    const resultado = this.parseLegacyLoanDetail(detalle) ?? { texto: detalle };
    this.detalleCache.set(detalle, resultado);
    return resultado;
  }

  private isAuditObservationDetail(
    value: unknown,
  ): value is AuditObservationDetail {
    if (!value || typeof value !== 'object') return false;

    const possibleDetail = value as AuditObservationDetail;

    return Boolean(
      possibleDetail.observacion ||
      possibleDetail.texto ||
      possibleDetail.equipos ||
      possibleDetail.usuarioNombre ||
      possibleDetail.equiposPrestamo,
    );
  }

  resumenObs(log: AuditLogDto): string {
    const p = this.parseDetalle(log.Detalle);
    if (!p) return '—';
    return (
      p.observacion ||
      p.texto ||
      (p.equiposPrestamo ? `Reserva de ${p.equiposPrestamo}` : undefined) ||
      (p.equipos?.length ? 'Ver estados de equipos' : '—')
    );
  }

  tieneDetalle(log: AuditLogDto): boolean {
    const p = this.parseDetalle(log.Detalle);
    return (
      !!p &&
      !!(p.observacion || p.texto || p.equiposPrestamo || p.equipos?.length)
    );
  }

  abrirObs(log: AuditLogDto): void {
    const p = this.parseDetalle(log.Detalle);
    if (p) this.obsAbierta = p;
  }

  detenerPropagacion(event: Event): void {
    event.stopPropagation();
  }

  cerrarObs(): void {
    this.obsAbierta = null;
  }

  formatearFechaDetalle(fecha?: string): string {
    if (!fecha) return '—';

    const value = new Date(fecha);
    if (Number.isNaN(value.getTime())) return fecha;

    return new Intl.DateTimeFormat('es-BO', {
      timeZone: 'America/La_Paz',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    }).format(value);
  }

  estadoEquipoLabel(estado?: string): string {
    switch (estado) {
      case 'operativo':
        return 'Operativo';
      case 'parcialmente_operativo':
        return 'Parcialmente operativo';
      case 'inoperativo':
        return 'Inoperativo';
      default:
        return estado || '—';
    }
  }

  estadoEquipoCssClass(estado?: string): string {
    return estado ? estado.replaceAll('_', '-') : 'none';
  }

  badgeClass(accion: string | undefined): string {
    switch (accion?.toLowerCase()) {
      case 'crear':
        return 'badge-aprobado';
      case 'editar':
        return 'badge-pendiente';
      case 'aprobar':
      case 'recoger':
        return 'badge-activo';
      case 'devolver':
        return 'badge-finalizado';
      case 'eliminar':
      case 'rechazar':
      case 'cancelar':
        return 'badge-rechazado';
      case 'atrasadoautomatico':
        return 'badge-atrasado';
      case 'registrarcontrato':
        return 'badge-aprobado';
      case 'eliminarcontrato':
        return 'badge-rechazado';
      default:
        return 'badge-cancelado';
    }
  }

  private auditSortValue(log: AuditLogDto, columna: string): unknown {
    const values: Record<string, unknown> = {
      Fecha: log.Timestamp,
      Actor: log.AdminNombre || log.AdminCarnet,
      Acción: log.Accion,
      ID: log.EntidadId,
      Observación: this.resumenObs(log),
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

  private parseLegacyLoanDetail(detail: string): AuditObservationDetail | null {
    const match = detail.match(
      /^Usuario:\s*(.+?)\s*\(([^)]+)\)\.\s*Equipos:\s*(.+?)\.\s*Inicio:\s*(.+?)\s+UTC\.\s*Devolución:\s*(.+?)\s+UTC\.?$/i,
    );

    if (!match) return null;

    return {
      usuarioNombre: match[1].trim(),
      usuarioCarnet: match[2].trim(),
      equiposPrestamo: match[3].trim(),
      fechaInicio: this.normalizarFechaUtcLegacy(match[4]),
      fechaDevolucion: this.normalizarFechaUtcLegacy(match[5]),
    };
  }

  private normalizarFechaUtcLegacy(value: string): string {
    const normalized = value.trim().replace(' ', 'T');
    return normalized.endsWith('Z') ? normalized : `${normalized}:00Z`;
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
