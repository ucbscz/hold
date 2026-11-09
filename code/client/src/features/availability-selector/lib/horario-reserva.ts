import { ConfiguracionDto, horarioParaFecha } from '@entities/configuracion';
import {
  MILLISECONDS_PER_DAY,
  MILLISECONDS_PER_MINUTE,
  comparaSoloFecha,
  limiteDelDia,
  minutosDelDia,
} from './fechas';

type Config = ConfiguracionDto | null;

export function minutosMinimos(config: Config): number {
  return config?.TiempoMinimoReservaMinutos ?? 30;
}

export function estaCerrado(config: Config, fecha: Date): boolean {
  return !horarioParaFecha(config, fecha).Abierto;
}

export function limiteHoraInicio(
  config: Config,
  fecha: Date,
  fin: Date | null,
): number {
  const mismoDia = !fin || comparaSoloFecha(fecha, fin) === 0;

  return (
    horarioParaFecha(config, fecha).FinMinutos -
    (mismoDia ? minutosMinimos(config) : 1)
  );
}

export function inicioDelSiguienteDia(config: Config, fecha: Date): Date {
  const siguiente = new Date(fecha);
  for (let intento = 0; intento < 374; intento++) {
    siguiente.setDate(siguiente.getDate() + 1);
    if (!estaCerrado(config, siguiente)) break;
  }
  return limiteDelDia(
    siguiente,
    horarioParaFecha(config, siguiente).InicioMinutos,
  );
}

export function siguienteBloque(config: Config, ahora: Date): Date {
  const fecha = new Date(ahora);
  fecha.setSeconds(0, 0);

  if (estaCerrado(config, fecha)) return inicioDelSiguienteDia(config, fecha);

  const apertura = horarioParaFecha(config, fecha).InicioMinutos;
  if (minutosDelDia(fecha) < apertura) return limiteDelDia(fecha, apertura);

  fecha.setMinutes(fecha.getMinutes() + 1);
  return minutosDelDia(fecha) >
    horarioParaFecha(config, fecha).FinMinutos - minutosMinimos(config)
    ? inicioDelSiguienteDia(config, fecha)
    : fecha;
}

export function limiteMaximoPrestamo(
  inicio: Date | null,
  maximoDias: number | null,
): Date | null {
  return inicio && maximoDias != null
    ? new Date(inicio.getTime() + maximoDias * MILLISECONDS_PER_DAY)
    : null;
}

export function normalizarInicio(
  config: Config,
  fecha: Date,
  minimo: Date,
  fin: Date | null,
): Date {
  let normalizada =
    fecha.getTime() < minimo.getTime() ? new Date(minimo) : new Date(fecha);
  const apertura = limiteDelDia(
    normalizada,
    horarioParaFecha(config, normalizada).InicioMinutos,
  );
  if (normalizada < apertura) normalizada = apertura;
  const limite = limiteDelDia(
    normalizada,
    limiteHoraInicio(config, normalizada, fin),
  );

  return normalizada.getTime() > limite.getTime()
    ? inicioDelSiguienteDia(config, normalizada)
    : normalizada;
}

export function normalizarFin(
  config: Config,
  fecha: Date,
  minimo: Date,
  limiteMaximo: Date | null,
): Date {
  let normalizada =
    fecha.getTime() < minimo.getTime() ? new Date(minimo) : new Date(fecha);
  const limite = limiteDelDia(
    normalizada,
    horarioParaFecha(config, normalizada).FinMinutos,
  );

  if (normalizada.getTime() > limite.getTime()) normalizada = limite;
  if (limiteMaximo && normalizada.getTime() > limiteMaximo.getTime())
    normalizada = limiteMaximo;

  return normalizada;
}

export function esRangoValido(
  config: Config,
  inicio: Date,
  fin: Date,
  maximoDias: number | null,
): boolean {
  const duracion = fin.getTime() - inicio.getTime();
  const apertura = horarioParaFecha(config, inicio);
  const cierre = horarioParaFecha(config, fin);
  const minutosInicio = minutosDelDia(inicio);
  const minutosFin = minutosDelDia(fin);
  const minimo = minutosMinimos(config);
  const limiteInicio =
    comparaSoloFecha(inicio, fin) === 0
      ? apertura.FinMinutos - minimo
      : apertura.FinMinutos - 1;
  return (
    duracion >= minimo * MILLISECONDS_PER_MINUTE &&
    apertura.Abierto &&
    cierre.Abierto &&
    minutosInicio >= apertura.InicioMinutos &&
    minutosInicio <= limiteInicio &&
    minutosFin >= cierre.InicioMinutos &&
    minutosFin <= cierre.FinMinutos &&
    (maximoDias == null || duracion <= maximoDias * MILLISECONDS_PER_DAY)
  );
}
