import { EquipoHistorialInlineItem } from '../model/equipo-historial-inline-item';
import { HistorialEquipoDto } from '../model/historial-equipo';

export function toHistorialEquipo(
  item: EquipoHistorialInlineItem,
): HistorialEquipoDto {
  return {
    IdPrestamo: item.IdPrestamo,
    NombreUsuario: item.NombreUsuario ?? undefined,
    Carnet: item.Carnet ?? undefined,
    EstadoPrestamo: item.EstadoPrestamo ?? undefined,
    EstadoEquipo: item.EstadoEquipo ?? undefined,
    Observacion: item.Observacion ?? undefined,
    FechaPrestamo: toDate(item.FechaPrestamo),
    FechaDevolucionEsperada: toDate(item.FechaDevolucionEsperada),
    FechaDevolucion: toDate(item.FechaDevolucion),
  };
}

function toDate(value: string | Date | null): Date | undefined {
  return value ? new Date(value) : undefined;
}
