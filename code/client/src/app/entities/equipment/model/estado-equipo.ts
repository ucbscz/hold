import { OpcionSelect } from '@shared/ui';

export const ESTADO_EQUIPO_OPCIONES: OpcionSelect[] = [
  { value: 'operativo', label: 'Operativo' },
  { value: 'parcialmente_operativo', label: 'Parcialmente operativo' },
  { value: 'inoperativo', label: 'Inoperativo' },
];

export function estadoEquipoLabel(estado?: string | null): string {
  return (
    ESTADO_EQUIPO_OPCIONES.find((opcion) => opcion.value === estado)?.label ??
    (estado || '—')
  );
}

export function estadoEquipoCssClass(estado?: string | null): string {
  return estado ? estado.replaceAll('_', '-') : 'none';
}
