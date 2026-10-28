import { OpcionSelect } from '@shared/ui';

export const ROL_ADMINISTRADOR = 'administrador';
export const ROL_ADMINISTRADOR_LABORATORIO = 'administrador_laboratorio';

const NOMBRES_ROL: Record<string, string> = {
  [ROL_ADMINISTRADOR]: 'Administrador general',
  [ROL_ADMINISTRADOR_LABORATORIO]: 'Administrador de laboratorio',
  administrativo: 'Administrativo',
  docente: 'Docente',
  estudiante: 'Estudiante',
};

export const ROL_OPCIONES: OpcionSelect[] = Object.entries(NOMBRES_ROL).map(
  ([value, label]) => ({ value, label }),
);

export function nombreRol(rol: string | null | undefined): string {
  return NOMBRES_ROL[rol?.toLowerCase() ?? ''] ?? rol ?? '';
}

export function esRolGestion(rol: string | null | undefined): boolean {
  const normalizado = rol?.toLowerCase();
  return (
    normalizado === ROL_ADMINISTRADOR ||
    normalizado === ROL_ADMINISTRADOR_LABORATORIO
  );
}

export function opcionesRolAsignables(
  rolActor: string | null | undefined,
): OpcionSelect[] {
  return rolActor?.toLowerCase() === ROL_ADMINISTRADOR
    ? ROL_OPCIONES
    : ROL_OPCIONES.filter((opcion) => !esRolGestion(String(opcion.value)));
}
