import { HttpParams } from '@angular/common/http';
import { EMPTY, Observable, expand, reduce } from 'rxjs';

export const TAMANO_PAGINA_API = 500;

export function parametrosPagina(
  pagina: number,
  tamano = TAMANO_PAGINA_API,
): HttpParams {
  return new HttpParams().set('pagina', pagina).set('tamanoPagina', tamano);
}

export function obtenerTodasLasPaginas<T>(
  obtenerPagina: (pagina: number) => Observable<T[]>,
  tamano = TAMANO_PAGINA_API,
  contar: (items: T[]) => number = (items) => items.length,
): Observable<T[]> {
  return obtenerPagina(1).pipe(
    expand((items, indice) =>
      contar(items) < tamano ? EMPTY : obtenerPagina(indice + 2),
    ),
    reduce((todos, items) => todos.concat(items), [] as T[]),
  );
}
