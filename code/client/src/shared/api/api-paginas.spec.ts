import { of } from 'rxjs';
import { obtenerTodasLasPaginas } from './api-paginas';

describe('obtenerTodasLasPaginas', () => {
  it('requests pages until one comes back incomplete', (done) => {
    const pedidas: number[] = [];
    const paginas: Record<number, number[]> = { 1: [1, 2], 2: [3, 4], 3: [5] };

    obtenerTodasLasPaginas((pagina) => {
      pedidas.push(pagina);
      return of(paginas[pagina] ?? []);
    }, 2).subscribe((todos) => {
      expect(todos).toEqual([1, 2, 3, 4, 5]);
      expect(pedidas).toEqual([1, 2, 3]);
      done();
    });
  });

  it('counts complete pages with a custom counter', (done) => {
    const paginas: Record<number, number[]> = { 1: [1, 1, 2], 2: [3] };

    obtenerTodasLasPaginas(
      (pagina) => of(paginas[pagina] ?? []),
      2,
      (items) => new Set(items).size,
    ).subscribe((todos) => {
      expect(todos).toEqual([1, 1, 2, 3]);
      done();
    });
  });
});
