import { PrestamoAgrupados } from '@entities/loan';
import { nombreRol, ROL_OPCIONES } from '@entities/user';
import { OpcionSelect } from '@shared/ui';
import { formatRegionDateTime } from '@shared/lib/date';

export const COLUMNAS_PRESTAMOS = [
  'Usuario',
  'Carnet',
  'Rol',
  'Teléfono',
  'Equipos',
  'Fecha Solicitud',
  'Fecha Préstamo Esperada',
  'Fecha Devolución Esperada',
] as const;

export const ROLES_FILTRO_PRESTAMOS: OpcionSelect[] = [
  { value: '', label: 'Todos los roles' },
  ...ROL_OPCIONES,
];

type FormatoFecha = (fecha: Date | string | null) => string;

export function estadoCalculado(
  prestamo: PrestamoAgrupados,
  ahora: Date = new Date(),
): string {
  const estado = (prestamo?.datosgrupo?.EstadoPrestamo || '').toLowerCase();
  const fechaDevolucion = prestamo?.datosgrupo?.FechaDevolucionEsperada;

  return fechaDevolucion &&
    (estado === 'activo' || estado === 'aprobado') &&
    fechaDevolucion < ahora
    ? 'atrasado'
    : estado;
}

export function camposBusqueda(
  prestamo: PrestamoAgrupados,
  columna: string,
  formatoFecha: FormatoFecha,
): unknown[] {
  const datos = prestamo.datosgrupo;
  const fechas = {
    'Fecha Solicitud': formatoFecha(datos.FechaSolicitud),
    'Fecha Préstamo Esperada': formatoFecha(datos.FechaPrestamoEsperada),
    'Fecha Devolución Esperada': formatoFecha(datos.FechaDevolucionEsperada),
  };

  switch (columna) {
    case 'Usuario':
      return [datos.NombreUsuario, datos.ApellidoPaternoUsuario];
    case 'Carnet':
      return [datos.CarnetUsuario];
    case 'Teléfono':
      return [datos.TelefonoUsuario];
    case 'Equipos':
      return [datos.NombreGrupoEquipo];
    case 'Fecha Solicitud':
    case 'Fecha Préstamo Esperada':
    case 'Fecha Devolución Esperada':
      return [fechas[columna]];
    default:
      return [
        datos.NombreUsuario,
        datos.ApellidoPaternoUsuario,
        datos.CarnetUsuario,
        datos.NombreGrupoEquipo,
        datos.CodigoImt,
        ...Object.values(fechas),
      ];
  }
}

export function enRangoDeFechas(
  prestamo: PrestamoAgrupados,
  desde: string,
  hasta: string,
): boolean {
  if (!desde && !hasta) return true;

  const fecha = prestamo.datosgrupo.FechaPrestamoEsperada;
  return (
    !!fecha &&
    (!desde || fecha >= new Date(`${desde}T00:00:00`)) &&
    (!hasta || fecha <= new Date(`${hasta}T23:59:59.999`))
  );
}

export function resumenEquipo(prestamo: PrestamoAgrupados): string {
  const primerEquipo = prestamo.equipos[0]?.NombreGrupoEquipo ?? '';

  return prestamo.equipos.length > 1 ? `${primerEquipo}...` : primerEquipo;
}

export function detalleEquipos(prestamo: PrestamoAgrupados): string {
  return prestamo.equipos
    .map((equipo) => equipo.NombreGrupoEquipo)
    .filter((nombre): nombre is string => !!nombre)
    .join(', ');
}

export function filaExportacion(prestamo: PrestamoAgrupados): unknown[] {
  const datos = prestamo.datosgrupo;
  return [
    `${datos.NombreUsuario ?? ''} ${datos.ApellidoPaternoUsuario ?? ''}`.trim(),
    datos.CarnetUsuario,
    nombreRol(datos.TipoUsuario),
    datos.TelefonoUsuario,
    detalleEquipos(prestamo),
    formatRegionDateTime(datos.FechaSolicitud),
    formatRegionDateTime(datos.FechaPrestamoEsperada),
    formatRegionDateTime(datos.FechaDevolucionEsperada),
    estadoCalculado(prestamo),
  ];
}

export const ORDEN_PRESTAMOS: Record<
  string,
  (fila: [number, PrestamoAgrupados]) => unknown
> = {
  Usuario: ([, prestamo]) =>
    `${prestamo.datosgrupo.NombreUsuario ?? ''} ${prestamo.datosgrupo.ApellidoPaternoUsuario ?? ''}`,
  Carnet: ([, prestamo]) => prestamo.datosgrupo.CarnetUsuario,
  Rol: ([, prestamo]) => nombreRol(prestamo.datosgrupo.TipoUsuario),
  Teléfono: ([, prestamo]) => prestamo.datosgrupo.TelefonoUsuario,
  Equipos: ([, prestamo]) => prestamo.equipos[0]?.NombreGrupoEquipo,
  'Fecha Solicitud': ([, prestamo]) => prestamo.datosgrupo.FechaSolicitud,
  'Fecha Préstamo Esperada': ([, prestamo]) =>
    prestamo.datosgrupo.FechaPrestamoEsperada,
  'Fecha Devolución Esperada': ([, prestamo]) =>
    prestamo.datosgrupo.FechaDevolucionEsperada,
  Estado: ([, prestamo]) => estadoCalculado(prestamo),
};
