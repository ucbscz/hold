import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UsuarioService, UsuarioServiceAPI } from '@entities/user';
import { AuthService } from '@features/auth-session';
import { MostrarerrorComponent } from '@shared/ui';
import { extractErrorMessage } from '@shared/lib/error';

const BAD_REQUEST_STATUS = 400;
const UNAUTHORIZED_STATUS = 401;

@Component({
  selector: 'app-iniciar-sesion',
  standalone: true,
  imports: [ValidatedFormsModule, CommonModule, MostrarerrorComponent],
  templateUrl: './iniciar-sesion.component.html',
  styleUrls: ['./iniciar-sesion.component.css'],
})
export class IniciarSesionComponent implements OnInit {
  email: string = '';
  contrasena: string = '';
  loading: boolean = false;
  incorrecto: boolean = false;
  requiereVerificacion = false;
  reenviando = false;
  mensajeError = 'Correo o contraseña incorrectos';
  mostrarContrasena = false;
  errorraro: WritableSignal<boolean> = signal(false);

  constructor(
    private readonly usuario: UsuarioService,
    private readonly router: Router,
    private readonly usuarioapi: UsuarioServiceAPI,
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const codigo = this.route.snapshot.queryParamMap.get('codigo');
    if (codigo) this.procesarCodigoGoogle(codigo);
    const googleError = this.route.snapshot.queryParamMap.get('googleError');
    if (googleError) {
      this.mensajeError =
        googleError === 'configuracion'
          ? 'El acceso con Google aún no está configurado. Usa tu correo y contraseña institucional.'
          : 'No se pudo iniciar sesión con Google. Usa una cuenta institucional e inténtalo nuevamente.';
      this.incorrecto = true;
    }
  }

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
        this.router.navigate([
          this.authService.isAdmin() ? '/administracion' : '/inicio',
        ]);
      },
      error: (error) => {
        this.mensajeError = extractErrorMessage(
          error,
          'Correo o contraseña incorrectos',
        );
        this.requiereVerificacion = this.mensajeError
          .toLowerCase()
          .includes('verificar');
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

  iniciarGoogle(): void {
    if (!this.loading) this.usuarioapi.iniciarSesionGoogle();
  }

  reenviarVerificacion(): void {
    if (this.reenviando || !this.email) return;
    this.reenviando = true;
    this.usuarioapi.reenviarVerificacion(this.email).subscribe({
      next: () => {
        this.mensajeError =
          'Si la cuenta está pendiente, recibirás un nuevo enlace de verificación.';
        this.reenviando = false;
      },
      error: () => {
        this.mensajeError =
          'No se pudo reenviar el correo. Intenta nuevamente.';
        this.reenviando = false;
      },
    });
  }

  private procesarCodigoGoogle(codigo: string): void {
    this.loading = true;
    this.usuarioapi.intercambiarCodigoGoogle(codigo).subscribe({
      next: (result) => {
        if (result.RequiereRegistro) {
          void this.router.navigate(['/registro'], {
            queryParams: { google: result.CodigoRegistro },
          });
          return;
        }
        if (!result.Sesion) {
          this.mostrarErrorGoogle();
          return;
        }
        const data = this.usuarioapi.mapearSesionGoogle(result.Sesion);
        this.authService.setSession(
          data.accessToken,
          data.refreshToken,
          data.usuario,
        );
        this.usuario.guardarSesion(data.usuario);
        void this.router.navigate([
          this.authService.isAdmin() ? '/administracion' : '/inicio',
        ]);
      },
      error: () => this.mostrarErrorGoogle(),
    });
  }

  private mostrarErrorGoogle(): void {
    this.loading = false;
    this.mensajeError =
      'El acceso con Google expiró o no pudo completarse. Intenta nuevamente.';
    this.incorrecto = true;
  }
  registrarUsuario(): void {
    this.router.navigate(['/registro']);
  }

  recuperarContrasena(): void {
    void this.router.navigate(['/recuperar-contrasena']);
  }

  alternarVisibilidadContrasena(): void {
    this.mostrarContrasena = !this.mostrarContrasena;
  }
}
