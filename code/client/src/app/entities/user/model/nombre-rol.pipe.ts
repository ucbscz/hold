import { Pipe, PipeTransform } from '@angular/core';

export function nombreRol(rol: string | null | undefined): string {
  const nombres: Record<string, string> = {
    administrador: 'Administrador general',
    administrador_laboratorio: 'Administrador de laboratorio',
    administrativo: 'Administrativo',
    docente: 'Docente',
    estudiante: 'Estudiante',
  };
  return nombres[rol?.toLowerCase() ?? ''] ?? rol ?? '';
}

@Pipe({ name: 'nombreRol' })
export class NombreRolPipe implements PipeTransform {
  transform(rol: string | null | undefined): string {
    return nombreRol(rol);
  }
}
