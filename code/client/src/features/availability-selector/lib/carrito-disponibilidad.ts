import { REGION } from '@shared/config';
import { Disponibilidad } from '@entities/availability';
import { Carrito } from '@entities/cart';

export function maximoDiasPrestamo(carrito: Carrito): number | null {
  const limits = Object.values(carrito)
    .map((item) => item.tiempoMaximoPrestamoDias)
    .filter((days) => Number.isFinite(days) && days > 0);
  return limits.length > 0 ? Math.min(...limits) : null;
}

export function cubreCarrito(
  carrito: Carrito,
  disponibilidad: Disponibilidad[],
): boolean {
  return Object.entries(carrito).every(([id, item]) => {
    const resultado = disponibilidad.find(
      (grupo) => grupo.IdGrupoEquipo === Number(id),
    );

    return (resultado?.CantidadDisponible ?? 0) >= item.cantidad;
  });
}

export function gruposSolicitados(carrito: Carrito) {
  return Object.entries(carrito)
    .map(([id, item]) => ({
      idGrupoEquipo: Number(id),
      cantidad: item.cantidad,
    }))
    .filter(
      (grupo) =>
        Number.isInteger(grupo.idGrupoEquipo) &&
        grupo.idGrupoEquipo > 0 &&
        grupo.cantidad > 0,
    );
}

export function etiquetaMes(mes: Date): string {
  return new Intl.DateTimeFormat(REGION.locale, {
    month: 'long',
    year: 'numeric',
  }).format(mes);
}

export function formatearFecha(fecha: Date | null): string {
  if (!fecha) return 'Sin seleccionar';
  return new Intl.DateTimeFormat(REGION.locale, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(fecha);
}
