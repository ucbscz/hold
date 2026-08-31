import { Injectable, signal, WritableSignal } from '@angular/core';
import { parseJsonResult } from '@shared/lib/result';
import {
  BrowserSessionStorageService,
  SESSION_STORAGE_KEYS,
} from '@shared/lib/session';
import { Usuario } from '../usuario';

@Injectable({
  providedIn: 'root',
})
export class UsuarioService {
  private readonly usuarioSignal: WritableSignal<Usuario> = signal(
    new Usuario(),
  );

  constructor(private readonly sessionStorage: BrowserSessionStorageService) {
    const storedJson = this.sessionStorage.getItem(SESSION_STORAGE_KEYS.user);

    if (!storedJson) return;

    const storedUserResult = parseJsonResult<Usuario>(storedJson);

    if (storedUserResult.isOk()) {
      this.usuarioSignal.set(storedUserResult.value);
      return;
    }

    this.sessionStorage.removeItem(SESSION_STORAGE_KEYS.user);
  }

  guardarSesion(usuario: Usuario): void {
    this.sessionStorage.setItem(
      SESSION_STORAGE_KEYS.user,
      JSON.stringify(usuario),
    );
    this.usuarioSignal.set(usuario);
  }

  estaVacio(): boolean {
    const usuario = this.usuarioSignal();
    return !usuario.nombre || usuario.nombre.trim() === '';
  }

  limpiarSesion(): void {
    this.sessionStorage.removeItem(SESSION_STORAGE_KEYS.user);
    this.usuarioSignal.set(new Usuario());
  }

  obtenerUsuario(): Usuario {
    return this.usuarioSignal();
  }

  actualizarUsuario(usuario: Usuario): void {
    this.usuarioSignal.set(usuario);
  }

  obtenerRol(): 'administrador' | 'usuario' {
    const usuario = this.usuarioSignal();
    return ['administrador', 'administrador_laboratorio'].includes(
      usuario.rol ?? '',
    )
      ? 'administrador'
      : 'usuario';
  }
}
