import { signal, WritableSignal } from '@angular/core';
export abstract class BaseTablaComponent {
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = 'Error desconocido , intente mas tarde';
  exito: WritableSignal<boolean> = signal(false);
  mensajeexito: string = 'Aviso informativo desconocido';
  aviso: WritableSignal<boolean> = signal(false);
  mensajeaviso: string = 'Aviso desconocido';
  procesando = false;

  protected iniciarEnvio(): boolean {
    if (this.procesando) return false;
    this.procesando = true;
    return true;
  }

  protected finalizarEnvio(): void {
    this.procesando = false;
  }
}
