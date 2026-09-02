import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Carrera } from '@entities/admin';
import { CarreraService } from '@entities/career';
import { Usuario, UsuarioService, UsuarioServiceAPI } from '@entities/user';
import { AuthService } from '@features/auth-session';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { switchMap } from 'rxjs';
@Component({
  selector: 'app-registrar-usuario',
  imports: [
    FormsModule,
    CommonModule,
    MostrarerrorComponent,
    AvisoExitoComponent,
    CustomSelectComponent,
    RouterLink,
  ],
  templateUrl: './registrar-usuario.component.html',
  styleUrl: './registrar-usuario.component.css',
})
export class RegistrarUsuarioComponent {
  nuevoUsuario: Usuario = new Usuario();
  password: string = '';
  confirmPassword: string = '';
  mostrarPassword = false;
  mostrarConfirmPassword = false;
  carreras: string[] = [];
  submitted: boolean = false;
  registrando: boolean = false;
  aceptaTerminos = false;
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = '';
  aviso: WritableSignal<boolean> = signal(false);
  mensajeaviso: string =
    'Aviso desconocido , si ve esto es un error , avise al soporte si puede o intente mas tarde';
  constructor(
    private readonly usuarioS: UsuarioService,
    private router: Router,
    private registrarcuenta: UsuarioServiceAPI,
    private carrerasS: CarreraService,
    private readonly authService: AuthService,
  ) {}
  ngOnInit() {
    this.carrerasS.obtenerCarreras().subscribe({
      next: (response: Carrera[]) => {
        this.carreras = response.map((carrera) => carrera.Nombre ?? '');
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al obtener las carreras intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  registrar(form: NgForm) {
    this.submitted = true;
    if (this.registrando) return;
    if (
      form.invalid ||
      this.password !== this.confirmPassword ||
      this.validartelefono(this.nuevoUsuario.telefono) ||
      !this.nuevoUsuario.carrera ||
      !this.aceptaTerminos
    ) {
      return;
    }
    const correo = this.nuevoUsuario.correo ?? '';
    let registroCompletado = false;

    this.registrando = true;
    this.error.set(false);
    this.aviso.set(false);
    this.nuevoUsuario.rol = 'usuario';
    this.registrarcuenta
      .registrarCuenta(
        this.nuevoUsuario,
        this.password,
        'estudiante',
        this.aceptaTerminos,
      )
      .pipe(
        switchMap(() => {
          registroCompletado = true;

          return this.registrarcuenta.iniciarSesion(correo, this.password);
        }),
      )
      .subscribe({
        next: (data) => {
          this.authService.setSession(
            data.accessToken,
            data.refreshToken,
            data.usuario,
          );
          this.usuarioS.guardarSesion(data.usuario);
          this.mensajeaviso = 'Usuario registrado exitosamente';
          this.aviso.set(true);
          this.registrando = false;
          this.router.navigate(['/inicio']);
        },
        error: (err) => {
          const mensajePorDefecto = registroCompletado
            ? 'Tu cuenta fue creada, pero no se pudo iniciar sesión automáticamente. Intenta iniciar sesión manualmente.'
            : 'Error al registrar el usuario intente mas tarde';
          const errorMsg = extractErrorMessage(err, mensajePorDefecto);
          this.mensajeerror = errorMsg;
          this.error.set(true);
          this.registrando = false;
        },
      });
  }
  irALogin() {
    this.router.navigate(['/login']);
  }

  irAHome() {
    this.router.navigate(['/inicio']);
  }

  alternarVisibilidadPassword(): void {
    this.mostrarPassword = !this.mostrarPassword;
  }

  alternarVisibilidadConfirmPassword(): void {
    this.mostrarConfirmPassword = !this.mostrarConfirmPassword;
  }

  validartelefono(telefono: string | null | undefined): boolean {
    const regex = /^[-+0-9]+$/;
    return !regex.test(<string>telefono);
  }
}
