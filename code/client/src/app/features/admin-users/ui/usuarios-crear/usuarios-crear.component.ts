import {
  Component,
  inject,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Usuario, UsuarioServiceAPI, UsuarioService } from '@entities/user';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
@Component({
  selector: 'app-usuarios-crear',
  standalone: true,
  imports: [
    FormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
  ],
  templateUrl: './usuarios-crear.component.html',
  styleUrl: './usuarios-crear.component.css',
})
export class UsuariosCrearComponent extends BaseTablaComponent {
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  @Input() carreras: string[] = [];
  usuario: Usuario = new Usuario();
  contrasena: string = '';
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
    this.mensajeaviso = '¿Desea crear el usuario ';
    this.aviso.set(true);
  }
  registrar() {
    this.usuarioApi
      .registrarCuenta(this.usuario, this.contrasena, this.usuario.rol!)
      .subscribe({
        next: (_response) => {
          this.Actualizar.emit();
          this.mensajeexito = 'Usuario creado exitosamente';
          this.exito.set(true);
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'No se pudo crear el usuario.',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
}
