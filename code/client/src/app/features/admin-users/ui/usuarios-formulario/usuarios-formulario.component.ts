import { Component, inject, Input } from '@angular/core';
import {
  opcionesRolAsignables,
  Usuario,
  UsuarioService,
  UsuarioServiceAPI,
} from '@entities/user';
import { FormularioAdminComponent } from '@shared/lib/admin-form';
import { ValidatedFormsModule, INSTITUTIONAL_EMAIL } from '@shared/lib/forms';
import {
  Aviso,
  CustomSelectComponent,
  MostrarerrorComponent,
  PasswordInputComponent,
} from '@shared/ui';

@Component({
  selector: 'app-usuarios-formulario',
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    CustomSelectComponent,
    PasswordInputComponent,
  ],
  templateUrl: './usuarios-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class UsuariosFormularioComponent extends FormularioAdminComponent<Usuario> {
  protected readonly correoInstitucional = INSTITUTIONAL_EMAIL;
  private readonly usuarioApi = inject(UsuarioServiceAPI);
  protected readonly entidad = { nombre: 'usuario' };
  @Input() carreras: string[] = [];
  readonly rolesOpciones = opcionesRolAsignables(
    inject(UsuarioService).obtenerUsuario().rol,
  );
  contrasena = '';
  repetirContrasena = '';

  get contrasenasValidas(): boolean {
    return (
      this.contrasena === this.repetirContrasena &&
      (this.editando || this.contrasena.length > 0)
    );
  }

  protected crearModelo(): Usuario {
    return new Usuario();
  }

  protected override validar(): string | null {
    return this.contrasenasValidas ? null : 'Las contraseñas deben coincidir.';
  }

  protected crear(usuario: Usuario) {
    return this.usuarioApi.registrarCuenta(
      usuario,
      this.contrasena,
      usuario.rol!,
    );
  }

  protected editar(usuario: Usuario) {
    return this.usuarioApi.editarUsuario(usuario, this.contrasena);
  }
}
