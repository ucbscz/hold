import { OpcionSelect } from '@shared/ui';

const ESTADOS_PRESTAMO = [
  'atrasado',
  'pendiente',
  'rechazado',
  'aprobado',
  'activo',
  'finalizado',
  'cancelado',
];

export const ESTADO_PRESTAMO_FILTRO_OPCIONES: OpcionSelect[] = [
  { value: '', label: 'Todos los estados' },
  ...ESTADOS_PRESTAMO.map((estado) => ({
    value: estado,
    label: estado.charAt(0).toUpperCase() + estado.slice(1),
  })),
];
