import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UsuarioService, UsuarioServiceAPI } from '@entities/user';
import { AuthService } from '@features/auth-session';
import { MostrarerrorComponent } from '@shared/ui';

const BAD_REQUEST_STATUS = 400;
const UNAUTHORIZED_STATUS = 401;

@Component({
  selector: 'app-iniciar-sesion',
  standalone: true,
  imports: [FormsModule, CommonModule, MostrarerrorComponent],
  templateUrl: './iniciar-sesion.component.html',
  styleUrls: ['./iniciar-sesion.component.css'],
})
export class IniciarSesionComponent {
  email: string = '';
  contrasena: string = '';
  loading: boolean = false;
  incorrecto: boolean = false;
  mostrarContrasena = false;
  errorraro: WritableSignal<boolean> = signal(false);

  constructor(
    private readonly usuario: UsuarioService,
    private readonly router: Router,
    private readonly usuarioapi: UsuarioServiceAPI,
    private readonly authService: AuthService,
  ) {}

  login(): void {
    if (this.loading) return;

    this.loading = true;
    this.usuarioapi.iniciarSesion(this.email, this.contrasena).subscribe({
      next: (data) => {
        this.authService.setSession(
          data.accessToken,
          data.refreshToken,
          data.usuario,
        );
        this.usuario.guardarSesion(data.usuario);

        this.loading = false;
        this.incorrecto = false;
        this.router.navigate(['/inicio']);
      },
      error: (error) => {
        if (
          error.status === BAD_REQUEST_STATUS ||
          error.status === UNAUTHORIZED_STATUS
        ) {
          this.incorrecto = true;
        } else {
          this.errorraro.set(true);
        }
        this.loading = false;
      },
    });
  }
  registrarUsuario(): void {
    this.router.navigate(['/registro']);
  }

  alternarVisibilidadContrasena(): void {
    this.mostrarContrasena = !this.mostrarContrasena;
  }
}
