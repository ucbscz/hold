import {
  Component,
  inject,
  EventEmitter,
  HostListener,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import {
  Usuario,
  UsuarioServiceAPI,
  UsuarioService,
  opcionesRolAsignables,
} from '@entities/user';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
  PasswordInputComponent,
  ToastService,
} from '@shared/ui';
@Component({
  selector: 'app-usuarios-editar',
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    CustomSelectComponent,
    PasswordInputComponent,
  ],
  templateUrl: './usuarios-editar.component.html',
  styleUrl: './usuarios-editar.component.css',
})
export class UsuariosEditarComponent extends BaseTablaComponent {
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Output() actualizar: EventEmitter<void> = new EventEmitter<void>();
  @Input() usuario: Usuario = new Usuario();
  @Input() carreras: string[] = [];
  contrasena: string = '';
  repetirContrasena = '';

  get contrasenasValidas(): boolean {
    return this.contrasena === this.repetirContrasena;
  }
  private readonly sesion = inject(UsuarioService);
  readonly rolesOpciones = opcionesRolAsignables(
    this.sesion.obtenerUsuario().rol,
  );
  constructor(
    private readonly usuarioApi: UsuarioServiceAPI,
    private readonly toast: ToastService,
  ) {
    super();
  }
  validareditar() {
    if (!this.contrasenasValidas) return;
    this.mensajeaviso = '¿Desea guardar los cambios realizados al usuario ?';
    this.aviso.set(true);
  }
  confirmar() {
    if (!this.contrasenasValidas || !this.iniciarEnvio()) return;
    this.usuarioApi.editarUsuario(this.usuario, this.contrasena).subscribe({
      next: (_response) => {
        this.cerrar();
        this.finalizarEnvio();
        this.toast.success('Usuario editado con éxito.');
        this.actualizar.emit();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al editar el usuario',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
        this.finalizarEnvio();
      },
    });
  }
  cerrar() {
    this.botoneditar.set(false);
  }
  @HostListener('click', ['$event'])
  onOverlayClick(event: MouseEvent) {
    if (event.target === event.currentTarget) this.cerrar();
  }
}
