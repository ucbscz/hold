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
import { Usuario, UsuarioServiceAPI, UsuarioService } from '@entities/user';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
  PasswordInputComponent,
} from '@shared/ui';
@Component({
  selector: 'app-usuarios-editar',
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
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
  readonly rolesOpciones: OpcionSelect[] = [
    { value: 'administrador', label: 'Administrador general' },
    {
      value: 'administrador_laboratorio',
      label: 'Administrador de laboratorio',
    },
    { value: 'administrativo', label: 'Administrativo' },
    { value: 'docente', label: 'Docente' },
    { value: 'estudiante', label: 'Estudiante' },
  ].filter(
    (rol) =>
      this.sesion.obtenerUsuario().rol?.toLowerCase() === 'administrador' ||
      !rol.value.startsWith('administrador'),
  );
  constructor(private readonly usuarioApi: UsuarioServiceAPI) {
    super();
  }
  validareditar() {
    if (!this.contrasenasValidas) return;
    this.mensajeaviso = '¿Desea guardar los cambios realizados al usuario ?';
    this.aviso.set(true);
  }
  confirmar() {
    if (!this.contrasenasValidas) return;
    this.usuarioApi.editarUsuario(this.usuario, this.contrasena).subscribe({
      next: (_response) => {
        this.actualizar.emit();
        this.mensajeexito = 'Usuario editado con exito';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al editar el usuario',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
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
