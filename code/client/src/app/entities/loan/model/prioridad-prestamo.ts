import { PrestamoDto } from '@entities/admin';

export function compararPrestamos(a: PrestamoDto, b: PrestamoDto): number {
  const estado = (p: PrestamoDto) => p.EstadoPrestamo?.toLowerCase() ?? '';
  const prioridad = (p: PrestamoDto) =>
    p.TipoUsuario?.toLowerCase() === 'docente'
      ? 0
      : p.TipoUsuario?.toLowerCase() === 'administrativo'
        ? 1
        : 2;
  return (
    Number(estado(a) !== 'pendiente') - Number(estado(b) !== 'pendiente') ||
    Number(['finalizado', 'cancelado', 'rechazado'].includes(estado(a))) -
      Number(['finalizado', 'cancelado', 'rechazado'].includes(estado(b))) ||
    prioridad(a) - prioridad(b) ||
    new Date(b.FechaSolicitud ?? 0).getTime() -
      new Date(a.FechaSolicitud ?? 0).getTime() ||
    b.Id - a.Id
  );
}
