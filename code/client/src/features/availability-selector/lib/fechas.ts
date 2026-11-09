export const MILLISECONDS_PER_MINUTE = 60 * 1000;
export const MILLISECONDS_PER_DAY = 24 * 60 * MILLISECONDS_PER_MINUTE;

export interface HoraOpcion {
  label: string;
  value: string;
}

export function dosDigitos(valor: number): string {
  return String(valor).padStart(2, '0');
}

export function horaTexto(fecha: Date): string {
  return `${dosDigitos(fecha.getHours())}:${dosDigitos(fecha.getMinutes())}`;
}

export function minutosDelDia(fecha: Date): number {
  return fecha.getHours() * 60 + fecha.getMinutes();
}

export function claveFecha(fecha: Date): string {
  return `${fecha.getFullYear()}-${dosDigitos(fecha.getMonth() + 1)}-${dosDigitos(fecha.getDate())}`;
}

export function crearHoras(): HoraOpcion[] {
  return Array.from({ length: 1440 }, (_, minutes) => {
    const value = `${dosDigitos(Math.floor(minutes / 60))}:${dosDigitos(minutes % 60)}`;
    return { value, label: value };
  });
}

export function combinarDiaYHora(dia: Date, hora: Date): Date {
  const resultado = new Date(dia);
  resultado.setHours(hora.getHours(), hora.getMinutes(), 0, 0);
  return resultado;
}

export function inicioDelMes(fecha: Date): Date {
  return new Date(fecha.getFullYear(), fecha.getMonth(), 1);
}

export function comparaSoloFecha(a: Date, b: Date): number {
  return (
    new Date(a.getFullYear(), a.getMonth(), a.getDate()).getTime() -
    new Date(b.getFullYear(), b.getMonth(), b.getDate()).getTime()
  );
}

export function sumarMinutos(fecha: Date, minutos: number): Date {
  return new Date(fecha.getTime() + minutos * MILLISECONDS_PER_MINUTE);
}

export function limiteDelDia(fecha: Date, minutos: number): Date {
  const limite = new Date(fecha);
  limite.setHours(Math.floor(minutos / 60), minutos % 60, 0, 0);
  return limite;
}

export function diasDelMes(mes: Date): Array<Date | null> {
  const primerDia = inicioDelMes(mes);
  const primerIndice = (primerDia.getDay() + 6) % 7;
  const dias: Array<Date | null> = Array.from(
    { length: primerIndice },
    () => null,
  );
  const ultimoDia = new Date(
    primerDia.getFullYear(),
    primerDia.getMonth() + 1,
    0,
  ).getDate();

  for (let dia = 1; dia <= ultimoDia; dia++) {
    dias.push(new Date(primerDia.getFullYear(), primerDia.getMonth(), dia));
  }

  while (dias.length % 7 !== 0) dias.push(null);
  return dias;
}
