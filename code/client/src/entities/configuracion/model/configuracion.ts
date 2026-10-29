export interface ConfiguracionDto {
  Horarios?: HorarioAtencion[];
  MontoMinimoContrato: number;
  HorarioInicioMinutos: number;
  HorarioFinMinutos: number;
  NombreJefeCarrera: string;
  CarnetJefeCarrera?: string | null;
  FirmaJefeCarreraBase64: string;
  TiempoMinimoReservaMinutos: number;
  TiempoRecordatorioPrevioMinutos: number;
  MinutosGraciaAtraso: number;
}

export interface HorarioAtencion {
  DiaSemana: number;
  Fecha?: string | null;
  Abierto: boolean;
  InicioMinutos: number;
  FinMinutos: number;
}

export function horarioParaFecha(
  config: ConfiguracionDto | null,
  fecha: Date,
): HorarioAtencion {
  const fechaTexto = `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, '0')}-${String(fecha.getDate()).padStart(2, '0')}`;
  return (
    config?.Horarios?.find((h) => h.Fecha === fechaTexto) ??
    config?.Horarios?.find(
      (h) => !h.Fecha && h.DiaSemana === fecha.getDay(),
    ) ?? {
      DiaSemana: fecha.getDay(),
      Abierto: fecha.getDay() !== 0,
      InicioMinutos: config?.HorarioInicioMinutos ?? 480,
      FinMinutos: config?.HorarioFinMinutos ?? 1080,
    }
  );
}

export const CONFIGURACION_PREDETERMINADA: ConfiguracionDto = {
  MontoMinimoContrato: 2000,
  HorarioInicioMinutos: 8 * 60,
  HorarioFinMinutos: 18 * 60,
  NombreJefeCarrera: '',
  FirmaJefeCarreraBase64: '',
  TiempoMinimoReservaMinutos: 30,
  TiempoRecordatorioPrevioMinutos: 30,
  MinutosGraciaAtraso: 15,
};
