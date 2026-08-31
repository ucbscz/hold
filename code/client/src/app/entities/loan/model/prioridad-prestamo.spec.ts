import { PrestamoDto } from '@entities/admin';
import { compararPrestamos } from './prioridad-prestamo';

describe('Prioridad de prestamos', () => {
  const prestamo = (
    Id: number,
    TipoUsuario: string,
    EstadoPrestamo = 'pendiente',
    fecha = '2026-08-29T12:00:00Z',
  ) =>
    Object.assign(new PrestamoDto(), {
      Id,
      TipoUsuario,
      EstadoPrestamo,
      FechaSolicitud: new Date(fecha),
    });

  it('prioriza pendientes, docentes, administrativos y estudiantes', () => {
    const filas = [
      prestamo(1, 'estudiante'),
      prestamo(2, 'docente', 'aprobado'),
      prestamo(3, 'administrativo'),
      prestamo(4, 'docente'),
    ];
    expect(filas.sort(compararPrestamos).map((p) => p.Id)).toEqual([
      4, 3, 1, 2,
    ]);
  });
  it('ordena mas recientes primero dentro del mismo rol y desempata por id', () => {
    const filas = [
      prestamo(1, 'docente'),
      prestamo(2, 'docente'),
      prestamo(3, 'docente', 'pendiente', '2026-08-30T12:00:00Z'),
    ];
    expect(filas.sort(compararPrestamos).map((p) => p.Id)).toEqual([3, 2, 1]);
  });
  it('muestra abiertos antes que cerrados incluso si el cerrado es de un docente', () => {
    expect(
      compararPrestamos(
        prestamo(1, 'estudiante', 'activo'),
        prestamo(2, 'docente', 'finalizado'),
      ),
    ).toBeLessThan(0);
  });
});
