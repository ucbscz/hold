import {
  CONFIGURACION_PREDETERMINADA,
  horarioParaFecha,
} from './configuracion';

describe('Horarios de atencion', () => {
  it('cierra los domingos y usa el horario global por defecto', () => {
    expect(horarioParaFecha(null, new Date(2026, 7, 30)).Abierto).toBeFalse();
    expect(horarioParaFecha(null, new Date(2026, 7, 31)).InicioMinutos).toBe(
      480,
    );
  });
  it('una excepcion tiene prioridad sobre el horario semanal', () => {
    const config = {
      ...CONFIGURACION_PREDETERMINADA,
      Horarios: [
        { DiaSemana: 1, Abierto: true, InicioMinutos: 600, FinMinutos: 900 },
        {
          DiaSemana: 1,
          Fecha: '2026-08-31',
          Abierto: false,
          InicioMinutos: 480,
          FinMinutos: 1080,
        },
      ],
    };
    expect(horarioParaFecha(config, new Date(2026, 7, 31)).Abierto).toBeFalse();
    expect(horarioParaFecha(config, new Date(2026, 8, 7)).InicioMinutos).toBe(
      600,
    );
  });
  it('permite una apertura especial de domingo configurada por root', () => {
    const config = {
      ...CONFIGURACION_PREDETERMINADA,
      Horarios: [
        {
          DiaSemana: 0,
          Fecha: '2026-08-30',
          Abierto: true,
          InicioMinutos: 600,
          FinMinutos: 720,
        },
      ],
    };
    expect(horarioParaFecha(config, new Date(2026, 7, 30)).Abierto).toBeTrue();
    expect(horarioParaFecha(config, new Date(2026, 8, 6)).Abierto).toBeFalse();
  });
});
