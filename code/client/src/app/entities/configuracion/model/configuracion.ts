export interface ConfiguracionDto {
  MontoMinimoContrato: number;
  HorarioInicioMinutos: number;
  HorarioFinMinutos: number;
  NombreJefeCarrera: string;
  FirmaJefeCarreraBase64: string;
  TiempoMinimoReservaMinutos: number;
  TiempoRecordatorioPrevioMinutos: number;
  MinutosGraciaAtraso: number;
}

export const CONFIGURACION_PREDETERMINADA: ConfiguracionDto = {
  MontoMinimoContrato: 2000,
  HorarioInicioMinutos: 8 * 60,
  HorarioFinMinutos: 18 * 60,
  NombreJefeCarrera: 'Job Angel Ledezma Dr.Ing',
  FirmaJefeCarreraBase64: '',
  TiempoMinimoReservaMinutos: 30,
  TiempoRecordatorioPrevioMinutos: 30,
  MinutosGraciaAtraso: 15,
};
