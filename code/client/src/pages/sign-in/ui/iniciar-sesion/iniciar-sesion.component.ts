import { EXTERNAL_SERVICES } from '@external-services/external-services';
import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { ValidatedFormsModule, INSTITUTIONAL_EMAIL } from '@shared/lib/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, UsuarioService, UsuarioServiceAPI } from '@entities/user';
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
  protected readonly proveedorIdentidad = EXTERNAL_SERVICES.identityProvider;
  protected readonly correoInstitucional = INSTITUTIONAL_EMAIL;
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
    if (codigo) this.procesarCodigoExterno(codigo);
    const errorExterno = this.route.snapshot.queryParamMap.get('errorExterno');
    if (errorExterno) {
      this.mensajeError =
        errorExterno === 'configuracion'
          ? `El acceso con ${EXTERNAL_SERVICES.identityProvider.name} aún no está configurado. Usa tu correo y contraseña institucional.`
          : `No se pudo iniciar sesión con ${EXTERNAL_SERVICES.identityProvider.name}. Usa una cuenta institucional e inténtalo nuevamente.`;
      this.incorrecto = true;
    }
  }

  login(): void {
    if (this.loading) return;

    this.loading = true;
    this.usuarioapi.iniciarSesion(this.email, this.contrasena).subscribe({
      next: (data) => {
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

  iniciarExterno(): void {
    if (!this.loading) this.usuarioapi.iniciarSesionExterna();
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

  private procesarCodigoExterno(codigo: string): void {
    this.loading = true;
    this.usuarioapi.intercambiarCodigoExterno(codigo).subscribe({
      next: (result) => {
        if (result.RequiereRegistro) {
          void this.router.navigate(['/registro'], {
            queryParams: { externo: result.CodigoRegistro },
          });
          return;
        }
        if (!result.Sesion) {
          this.mostrarErrorExterno();
          return;
        }
        const data = this.usuarioapi.mapearSesionExterna(result.Sesion);
        this.usuario.guardarSesion(data.usuario);
        void this.router.navigate([
          this.authService.isAdmin() ? '/administracion' : '/inicio',
        ]);
      },
      error: () => this.mostrarErrorExterno(),
    });
  }

  private mostrarErrorExterno(): void {
    this.loading = false;
    this.mensajeError = `El acceso con ${EXTERNAL_SERVICES.identityProvider.name} expiró o no pudo completarse. Intenta nuevamente.`;
    this.incorrecto = true;
  }
  registrarUsuario(): void {
    this.router.navigate(['/registro']);
  }

  recuperarContrasena(): void {
    void this.router.navigate(['/recuperar']);
  }

  alternarVisibilidadContrasena(): void {
    this.mostrarContrasena = !this.mostrarContrasena;
  }
}
