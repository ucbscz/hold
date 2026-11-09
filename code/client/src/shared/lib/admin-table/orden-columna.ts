import { AdminTableSort } from './admin-table-sort';

export type AriaOrden = 'none' | 'ascending' | 'descending';

export function siguienteDireccion(
  columnaActual: string,
  direccionActual: AdminTableSort['dir'],
  columna: string,
): AdminTableSort['dir'] {
  return columnaActual === columna && direccionActual === 'asc'
    ? 'desc'
    : 'asc';
}

export function iconoOrden(
  ordenada: boolean,
  direccion: AdminTableSort['dir'],
): string {
  if (!ordenada) return 'fa-sort';

  return direccion === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
}

export function ariaOrden(
  ordenada: boolean,
  direccion: AdminTableSort['dir'],
): AriaOrden {
  if (!ordenada) return 'none';

  return direccion === 'asc' ? 'ascending' : 'descending';
}
