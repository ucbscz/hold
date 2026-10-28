import { Pipe, PipeTransform } from '@angular/core';
import { nombreRol } from './rol';

@Pipe({ name: 'nombreRol' })
export class NombreRolPipe implements PipeTransform {
  transform(rol: string | null | undefined): string {
    return nombreRol(rol);
  }
}
