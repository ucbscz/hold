import { AdminTableSort } from './admin-table-sort';
import { AdminTableTab } from './admin-table-tab';
import { BaseTablaComponent } from './base';

export abstract class Tabla extends BaseTablaComponent {
  public columnas: string[] = [];
  activeTab: AdminTableTab = 'tabla';
  sortColumn = '';
  sortDirection: AdminTableSort['dir'] = 'asc';
  paginaActual = 1;
  readonly filasPorPagina = 10;
  private busquedaPersistida: [string, string] = ['', ''];

  seleccionarTab(tab: AdminTableTab): void {
    this.activeTab = tab;
  }

  estaTabActiva(tab: AdminTableTab): boolean {
    return this.activeTab === tab;
  }

  protected formatDate(date: Date | string | null): string {
    if (!date) return '';

    const parsedDate = new Date(date);

    if (Number.isNaN(parsedDate.getTime())) return '';

    const localDateStr = this.toLocalISOString(parsedDate);
    const [year, month, day] = localDateStr.split('-');

    return `${day}/${month}/${year}`;
  }

  protected normalizeText(text: unknown): string {
    return String(text ?? '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  ordenarPorColumna(columna: string): void {
    const columnaOrdenable = columna.trim();

    if (!columnaOrdenable) return;

    this.sortDirection =
      this.sortColumn === columnaOrdenable && this.sortDirection === 'asc'
        ? 'desc'
        : 'asc';
    this.sortColumn = columnaOrdenable;
    this.sortTable({ col: columnaOrdenable, dir: this.sortDirection });
  }

  protected aplicarOrdenActualSiExiste(): void {
    if (!this.sortColumn) return;

    this.sortTable({ col: this.sortColumn, dir: this.sortDirection });
  }

  paginar<T>(items: readonly T[]): T[] {
    const totalPaginas = Math.max(
      1,
      Math.ceil(items.length / this.filasPorPagina),
    );
    const pagina = Math.min(Math.max(1, this.paginaActual), totalPaginas);
    const inicio = (pagina - 1) * this.filasPorPagina;
    return items.slice(inicio, inicio + this.filasPorPagina);
  }

  cambiarPagina(pagina: number): void {
    this.paginaActual = Math.max(1, pagina);
  }

  reiniciarPaginacion(): void {
    this.paginaActual = 1;
  }

  protected mantenerBusqueda(event?: [string, string]): [string, string] {
    if (event) this.busquedaPersistida = event;

    return this.busquedaPersistida;
  }

  protected limpiarBusquedaPersistida(): void {
    this.busquedaPersistida = ['', ''];
  }

  exportarCsv(
    nombreArchivo: string,
    encabezados: string[],
    filas: unknown[][],
  ): void {
    const escape = (value: unknown) =>
      `"${String(value ?? '').replaceAll('"', '""')}"`;
    const csv = [encabezados, ...filas]
      .map((fila) => fila.map(escape).join(','))
      .join('\n');
    const enlace = document.createElement('a');
    enlace.href = URL.createObjectURL(
      new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8' }),
    );
    enlace.download = `${nombreArchivo}.csv`;
    enlace.click();
    URL.revokeObjectURL(enlace.href);
  }

  esColumnaOrdenada(columna: string): boolean {
    return this.sortColumn === columna.trim();
  }

  iconoOrdenColumna(columna: string): string {
    if (!this.esColumnaOrdenada(columna)) return 'fa-sort';

    return this.sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
  }

  ariaOrdenColumna(columna: string): 'none' | 'ascending' | 'descending' {
    if (!this.esColumnaOrdenada(columna)) return 'none';

    return this.sortDirection === 'asc' ? 'ascending' : 'descending';
  }

  protected sortByText<T>(
    items: readonly T[],
    direction: AdminTableSort['dir'],
    accessor: (item: T) => unknown,
  ): T[] {
    return this.sortByAccessor(items, direction, accessor, (first, second) =>
      this.compareTextValues(first, second),
    );
  }

  protected sortByNumber<T>(
    items: readonly T[],
    direction: AdminTableSort['dir'],
    accessor: (item: T) => unknown,
  ): T[] {
    return this.sortByAccessor(items, direction, accessor, (first, second) =>
      this.compareNumberValues(first, second),
    );
  }

  protected sortByDate<T>(
    items: readonly T[],
    direction: AdminTableSort['dir'],
    accessor: (item: T) => unknown,
  ): T[] {
    return this.sortByAccessor(items, direction, accessor, (first, second) =>
      this.compareDateValues(first, second),
    );
  }

  protected sortByAuto<T>(
    items: readonly T[],
    direction: AdminTableSort['dir'],
    accessor: (item: T) => unknown,
  ): T[] {
    return this.sortByAccessor(items, direction, accessor, (first, second) =>
      this.compareAutoValues(first, second),
    );
  }

  protected sortByColumn<T>(
    items: readonly T[],
    sort: AdminTableSort,
    accessors: Record<string, (item: T) => unknown>,
  ): T[] {
    const value = accessors[sort.col];

    if (!value) return [...items];

    return this.sortByAuto(items, sort.dir, value);
  }

  abstract aplicarFiltros(event?: [string, string]): void;

  sortTable(_event: AdminTableSort): void {}

  protected toLocalISOString(date: Date): string {
    const offset = date.getTimezoneOffset();
    const localDate = new Date(date.getTime() - offset * 60000);

    return localDate.toISOString().split('T')[0];
  }

  private sortByAccessor<T>(
    items: readonly T[],
    direction: AdminTableSort['dir'],
    accessor: (item: T) => unknown,
    compare: (firstValue: unknown, secondValue: unknown) => number,
  ): T[] {
    return [...items].sort((firstItem, secondItem) => {
      const result = compare(accessor(firstItem), accessor(secondItem));

      return direction === 'asc' ? result : -result;
    });
  }

  private compareAutoValues(firstValue: unknown, secondValue: unknown): number {
    const firstDate = this.toComparableTime(firstValue);
    const secondDate = this.toComparableTime(secondValue);

    if (Number.isFinite(firstDate) && Number.isFinite(secondDate))
      return this.compareDateValues(firstValue, secondValue);

    const firstNumber = this.toComparableNumber(firstValue);
    const secondNumber = this.toComparableNumber(secondValue);

    if (Number.isFinite(firstNumber) && Number.isFinite(secondNumber))
      return this.compareNumberValues(firstValue, secondValue);

    return this.compareTextValues(firstValue, secondValue);
  }

  private compareTextValues(firstValue: unknown, secondValue: unknown): number {
    return this.normalizeText(firstValue).localeCompare(
      this.normalizeText(secondValue),
      undefined,
      { numeric: true, sensitivity: 'base' },
    );
  }

  private compareNumberValues(
    firstValue: unknown,
    secondValue: unknown,
  ): number {
    const firstNumber = this.toComparableNumber(firstValue);
    const secondNumber = this.toComparableNumber(secondValue);

    if (!Number.isFinite(firstNumber) && !Number.isFinite(secondNumber))
      return 0;
    if (!Number.isFinite(firstNumber)) return 1;
    if (!Number.isFinite(secondNumber)) return -1;

    return firstNumber - secondNumber;
  }

  private compareDateValues(firstValue: unknown, secondValue: unknown): number {
    const firstDate = this.toComparableTime(firstValue);
    const secondDate = this.toComparableTime(secondValue);

    if (!Number.isFinite(firstDate) && !Number.isFinite(secondDate)) return 0;
    if (!Number.isFinite(firstDate)) return 1;
    if (!Number.isFinite(secondDate)) return -1;

    return firstDate - secondDate;
  }

  private toComparableNumber(value: unknown): number {
    if (typeof value === 'number') return value;
    if (typeof value !== 'string') return NaN;

    const normalizedValue = value.trim();

    if (!normalizedValue) return NaN;

    return Number(normalizedValue);
  }

  private toComparableTime(value: unknown): number {
    if (value instanceof Date) return value.getTime();

    if (typeof value !== 'string') return NaN;

    const normalizedValue = value.trim();

    if (!normalizedValue) return NaN;
    if (Number.isFinite(Number(normalizedValue))) return NaN;

    return Date.parse(normalizedValue);
  }
}
