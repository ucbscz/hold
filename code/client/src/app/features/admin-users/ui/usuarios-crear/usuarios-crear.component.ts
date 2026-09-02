import {
  Component,
  inject,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Usuario, UsuarioServiceAPI, UsuarioService } from '@entities/user';
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
  selector: 'app-usuarios-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    CustomSelectComponent,
    PasswordInputComponent,
  ],
  templateUrl: './usuarios-crear.component.html',
  styleUrl: './usuarios-crear.component.css',
})
export class UsuariosCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  @Input() carreras: string[] = [];
  usuario: Usuario = new Usuario();
  contrasena: string = '';
  repetirContrasena = '';

  get contrasenasValidas(): boolean {
    return (
      this.contrasena.length > 0 && this.contrasena === this.repetirContrasena
    );
  }
  private readonly sesion = inject(UsuarioService);
  readonly rolesOpciones: OpcionSelect[] = [
    { value: 'administrador', label: 'Administrador general' },
    {
      value: 'administrador_laboratorio',
      label: 'Administrador de laboratorio',
    },
    { value: 'administrativo', label: 'Administrativo' },
    { value: 'estudiante', label: 'Estudiante' },
    { value: 'docente', label: 'Docente' },
  ].filter(
    (rol) =>
      this.sesion.obtenerUsuario().rol?.toLowerCase() === 'administrador' ||
      !rol.value.startsWith('administrador'),
  );
  constructor(private readonly usuarioApi: UsuarioServiceAPI) {
    super();
  }
  validarcrear() {
    if (!this.contrasenasValidas) return;
    this.mensajeaviso = '¿Desea crear el usuario ';
    this.aviso.set(true);
  }
  registrar() {
    if (!this.contrasenasValidas || !this.iniciarEnvio()) return;
    this.usuarioApi
      .registrarCuenta(this.usuario, this.contrasena, this.usuario.rol!)
      .subscribe({
        next: (_response) => {
          this.Actualizar.emit();
          this.finalizarEnvio();
          this.toast.success('Usuario creado exitosamente.');
          this.cerrar();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'No se pudo crear el usuario.',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
          this.finalizarEnvio();
        },
      });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
}
