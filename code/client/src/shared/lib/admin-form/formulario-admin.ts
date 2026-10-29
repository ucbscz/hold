import {
  Directive,
  EventEmitter,
  HostListener,
  inject,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { Observable } from 'rxjs';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { ToastService } from '@shared/ui';
import { EntidadFormulario, textosFormulario } from './textos-formulario';

@Directive()
export abstract class FormularioAdminComponent<
  T extends object,
> extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
  @Input() abierto: WritableSignal<boolean> = signal(true);
  @Output() guardado = new EventEmitter<void>();
  modelo: T = this.crearModelo();
  editando = false;

  protected abstract readonly entidad: EntidadFormulario;

  @Input() set registro(registro: T | null | undefined) {
    if (!registro) return;
    this.modelo = { ...registro };
    this.editando = true;
  }

  get textos() {
    return textosFormulario(this.entidad, this.editando);
  }

  validarEnvio(): void {
    const problema = this.validar();
    if (problema) {
      this.mostrarError(problema);
      return;
    }
    this.mensajeaviso = this.textos.confirmacion;
    this.aviso.set(true);
  }

  confirmar(): void {
    if (this.validar() || !this.iniciarEnvio()) return;
    const envio = this.editando
      ? this.editar(this.modelo)
      : this.crear(this.modelo);
    envio.subscribe({
      next: () => {
        this.finalizarEnvio();
        this.cerrar();
        this.toast.success(this.textos.exito);
        this.guardado.emit();
      },
      error: (error) => {
        this.finalizarEnvio();
        this.mostrarError(extractErrorMessage(error, this.textos.fallo));
      },
    });
  }

  cerrar(): void {
    this.abierto.set(false);
  }

  @HostListener('click', ['$event'])
  cerrarDesdeFondo(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.cerrar();
  }

  protected cargarOpciones<R>(
    fuente: Observable<R>,
    asignar: (datos: R) => void,
    fallo: string,
  ): void {
    fuente.subscribe({
      next: asignar,
      error: (error) => this.mostrarError(extractErrorMessage(error, fallo)),
    });
  }

  protected mostrarError(mensaje: string): void {
    this.mensajeerror = mensaje;
    this.error.set(true);
  }

  protected validar(): string | null {
    return null;
  }

  protected abstract crearModelo(): T;
  protected abstract crear(modelo: T): Observable<unknown>;
  protected abstract editar(modelo: T): Observable<unknown>;
}
