import { Tabla } from './tabla';
import { AdminTableSort } from './admin-table-sort';

interface SortableItem {
  name?: string;
  amount?: number | string;
  date?: Date | string;
}

class TestTabla extends Tabla {
  aplicarFiltros(): void {}

  sortItems(
    items: readonly SortableItem[],
    sort: AdminTableSort,
    accessors: Record<string, (item: SortableItem) => unknown>,
  ): SortableItem[] {
    return this.sortByColumn(items, sort, accessors);
  }
}

describe('Tabla', () => {
  let tabla: TestTabla;

  beforeEach(() => {
    tabla = new TestTabla();
  });

  it('should sort text globally in both directions', () => {
    const items: SortableItem[] = [{ name: 'Zeta' }, { name: 'Álgebra' }];
    const accessors = { Nombre: (item: SortableItem) => item.name };

    expect(
      tabla
        .sortItems(items, { col: 'Nombre', dir: 'asc' }, accessors)
        .map((item) => item.name),
    ).toEqual(['Álgebra', 'Zeta']);
    expect(
      tabla
        .sortItems(items, { col: 'Nombre', dir: 'desc' }, accessors)
        .map((item) => item.name),
    ).toEqual(['Zeta', 'Álgebra']);
  });

  it('should sort numbers globally in both directions', () => {
    const items: SortableItem[] = [{ amount: '200' }, { amount: '100' }];
    const accessors = { Carnet: (item: SortableItem) => item.amount };

    expect(
      tabla
        .sortItems(items, { col: 'Carnet', dir: 'asc' }, accessors)
        .map((item) => item.amount),
    ).toEqual(['100', '200']);
    expect(
      tabla
        .sortItems(items, { col: 'Carnet', dir: 'desc' }, accessors)
        .map((item) => item.amount),
    ).toEqual(['200', '100']);
  });

  it('should sort dates globally in both directions', () => {
    const oldestDate = new Date('2026-06-12T18:21:00');
    const newestDate = new Date('2026-06-14T12:38:00');
    const items: SortableItem[] = [{ date: oldestDate }, { date: newestDate }];
    const accessors = { Fecha: (item: SortableItem) => item.date };

    expect(
      tabla
        .sortItems(items, { col: 'Fecha', dir: 'asc' }, accessors)
        .map((item) => (item.date as Date).getTime()),
    ).toEqual([oldestDate.getTime(), newestDate.getTime()]);
    expect(
      tabla
        .sortItems(items, { col: 'Fecha', dir: 'desc' }, accessors)
        .map((item) => (item.date as Date).getTime()),
    ).toEqual([newestDate.getTime(), oldestDate.getTime()]);
  });

  it('should export CSV files with an UTF-8 byte order mark', async () => {
    let exportedBlob: Blob | undefined;
    spyOn(URL, 'createObjectURL').and.callFake((blob) => {
      exportedBlob = blob as Blob;
      return 'blob:test';
    });
    spyOn(URL, 'revokeObjectURL');
    spyOn(HTMLAnchorElement.prototype, 'click');

    tabla.exportarCsv('prestamos', ['ID'], [['—']]);

    expect(exportedBlob).toBeDefined();
    const bytes = new Uint8Array(await exportedBlob!.arrayBuffer());
    expect(Array.from(bytes.slice(0, 3))).toEqual([0xef, 0xbb, 0xbf]);
    expect(new TextDecoder().decode(bytes)).toContain('"—"');
  });
});
