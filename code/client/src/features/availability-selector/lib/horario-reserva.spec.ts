import { esRangoValido, siguienteBloque } from './horario-reserva';

describe('horario-reserva', () => {
  it('defaults to the nearest valid minute after the current time', () => {
    const next = siguienteBloque(null, new Date(2030, 0, 10, 10, 12, 30));

    expect(next.getHours()).toBe(10);
    expect(next.getMinutes()).toBe(13);
  });

  it('rejects ranges shorter than the minimum duration', () => {
    const inicio = new Date(2030, 0, 10, 10, 0);

    expect(
      esRangoValido(null, inicio, new Date(2030, 0, 10, 10, 15), null),
    ).toBeFalse();
    expect(
      esRangoValido(null, inicio, new Date(2030, 0, 10, 11, 0), null),
    ).toBeTrue();
  });
});
