import { INVENTORY_LABELS } from '@brand/brand';
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
import { printTable, TablePaginationComponent } from '@shared/lib/admin-table';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import { parseJsonResult } from '@shared/lib/result';
import { formatBoliviaDateTime } from '@shared/lib/date';
import { AuditObservationDetail } from '../../model/audit-observation-detail';

const CAMPO_CODIGO_INTERNO = 'Código interno';
const INTERVALO_ACTUALIZACION_MS = 30000;

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
        formatBoliviaDateTime(log.Timestamp),
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

    this.sortDirection =
      this.sortColumn === columnaOrdenable && this.sortDirection === 'asc'
        ? 'desc'
        : 'asc';
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

    if (parsedDetail.isOk() && this.isRecord(parsedDetail.value)) {
      const structuredDetail = this.normalizarDetalle(parsedDetail.value);
      this.detalleCache.set(detalle, structuredDetail);
      return structuredDetail;
    }

    const resultado = this.parseLegacyLoanDetail(detalle) ?? { texto: detalle };
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
      formatBoliviaDateTime(fecha) ||
      (fecha instanceof Date ? fecha.toISOString() : fecha)
    );
  }

  protected readonly estadoEquipoLabel = estadoEquipoLabel;
  protected readonly estadoEquipoCssClass = estadoEquipoCssClass;

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

  accionLabel(accion?: string): string {
    if (!accion) return 'Acción registrada';

    return accion
      .replace(/([a-záéíóúñ])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
      .replace(/^./, (value) => value.toUpperCase());
  }

  entidadLabel(entidad?: string): string {
    const labels: Record<string, string> = {
      Prestamo: 'Préstamo',
      Usuario: 'Usuario',
      Equipo: 'Equipo',
      GrupoEquipo: 'Grupo de equipos',
      Accesorio: 'Accesorio',
      Componente: 'Componente',
      Gavetero: 'Gavetero',
      Mueble: 'Mueble',
      Mantenimiento: 'Mantenimiento',
      EmpresaMantenimiento: 'Empresa de mantenimiento',
      Carrera: 'Carrera',
      Categoria: 'Categoría',
      Ambiente: 'Ambiente',
      Procedencia: 'Procedencia',
      ConfiguracionSistema: 'Configuración del sistema',
    };

    return labels[entidad ?? ''] ?? entidad ?? this.entidad;
  }

  registroLabel(log: AuditLogDto): string {
    if (log.EntidadNombre?.trim()) return log.EntidadNombre.trim();

    return this.nombreDesdeDetalle(log) || this.entidadLabel(log.Entidad);
  }

  descripcionAccion(log: AuditLogDto): string {
    const entidad = this.entidadLabel(log.Entidad).toLowerCase();
    const registro = ` “${this.registroLabel(log)}”`;
    const action = log.Accion?.toLowerCase();
    const descriptions: Record<string, string> = {
      crear: `Se creó el registro de ${entidad}${registro}.`,
      editar: `Se actualizaron los datos de ${entidad}${registro}.`,
      eliminar: `Se eliminó el registro de ${entidad}${registro}.`,
      aprobar: `Se aprobó el ${entidad}${registro}.`,
      rechazar: `Se rechazó el ${entidad}${registro}.`,
      recoger: `Se registró la entrega del ${entidad}${registro}.`,
      devolver: `Se registró la devolución del ${entidad}${registro}.`,
      cancelar: `Se canceló el ${entidad}${registro}.`,
      bloquear: `Se bloqueó el registro de ${entidad}${registro}.`,
      desbloquear: `Se desbloqueó el registro de ${entidad}${registro}.`,
      atrasadoautomatico: `El sistema marcó el ${entidad}${registro} como atrasado.`,
      registrarcontrato: `Se registró el contrato del ${entidad}${registro}.`,
      eliminarcontrato: `Se eliminó el contrato del ${entidad}${registro}.`,
      eliminarcomentario: `Se eliminó un comentario de ${entidad}${registro}.`,
    };

    return (
      descriptions[action ?? ''] ??
      `Se registró una acción en ${entidad}${registro}.`
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

  private normalizarDetalle(
    value: Record<string, unknown>,
  ): AuditObservationDetail {
    const detail = value as unknown as AuditObservationDetail;
    const legacy =
      typeof detail.texto === 'string'
        ? this.parseLegacyLoanDetail(detail.texto)
        : null;

    if (legacy) return legacy;

    const ignored = new Set([
      'observacion',
      'texto',
      'equipos',
      'usuarioNombre',
      'usuarioCarnet',
      'equiposPrestamo',
      'fechaInicio',
      'fechaDevolucion',
      'cambios',
    ]);
    const datos = Object.entries(value)
      .filter(([key]) => !ignored.has(key) && !this.esClaveSensible(key))
      .map(([key, item]) => ({
        etiqueta: this.etiquetaDato(key),
        valor: this.formatearDato(item),
      }))
      .filter((item) => item.valor !== '');

    return {
      ...detail,
      datos: datos.length ? datos : undefined,
    };
  }

  private isRecord(value: unknown): value is Record<string, unknown> {
    return !!value && typeof value === 'object' && !Array.isArray(value);
  }

  private esClaveSensible(key: string): boolean {
    return /(contrasena|password|token|hash|secret|imagen|firma|googleid|refresh)/i.test(
      key,
    );
  }

  private etiquetaDato(key: string): string {
    const labels: Record<string, string> = {
      cuentaRecreada: 'Cuenta recreada',
      aceptoTerminos: 'Aceptó términos',
      versionTerminos: 'Versión de términos',
      fechaAceptacion: 'Fecha de aceptación',
      anterior: 'Valor anterior',
    };

    return (
      labels[key] ??
      key
        .replace(/([a-záéíóúñ])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
        .replace(/^./, (value) => value.toUpperCase())
    );
  }

  private formatearDato(value: unknown): string {
    if (value === null || value === undefined || value === '')
      return 'Sin valor';
    if (typeof value === 'boolean') return value ? 'Sí' : 'No';
    if (typeof value === 'string' || typeof value === 'number')
      return String(value);
    if (Array.isArray(value) && value.every((item) => typeof item !== 'object'))
      return value.join(', ');

    return '';
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
