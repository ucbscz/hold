import { INSTITUTIONAL_EMAIL } from '@shared/lib/forms';
import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { UsuarioServiceAPI } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';
import { ToastService } from '@shared/ui';

@Component({
  selector: 'app-recuperar-contrasena',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recuperar-contrasena.component.html',
  styleUrl: './recuperar-contrasena.component.css',
})
export class RecuperarContrasenaComponent {
  protected readonly correoInstitucional = INSTITUTIONAL_EMAIL;
  email = '';
  codigo = '';
  contrasena = '';
  repetirContrasena = '';
  enviando = false;
  codigoEnviado = false;
  error = '';
  mostrarContrasena = false;
  mostrarRepetirContrasena = false;

  constructor(
    private readonly router: Router,
    private readonly users: UsuarioServiceAPI,
    private readonly toast: ToastService,
  ) {}

  solicitar(): void {
    if (this.enviando || !this.email) return;

    this.enviando = true;
    this.error = '';
    this.users.solicitarRecuperacionContrasena(this.email).subscribe({
      next: () => {
        this.codigoEnviado = true;
        this.enviando = false;
        this.toast.success(
          'Correo enviado. Ingresa el código de recuperación para continuar.',
        );
      },
      error: (error) => {
        this.enviando = false;
        if (error.status === 404) {
          this.toast.error('No encontramos una cuenta con ese correo.');
          void this.router.navigate(['/login']);
          return;
        }
        this.toast.error(
          extractErrorMessage(
            error,
            'No se pudo enviar el correo. Intenta nuevamente.',
          ),
        );
      },
    });
  }

  restablecer(): void {
    if (this.enviando || !this.codigo) return;

    this.error = this.validarContrasena();
    if (this.error) return;

    this.enviando = true;
    this.users
      .restablecerContrasena(this.email, this.codigo, this.contrasena)
      .subscribe({
        next: () => {
          this.enviando = false;
          this.toast.success(
            'Contraseña actualizada. Ya puedes iniciar sesión.',
          );
          void this.router.navigate(['/login']);
        },
        error: (error) => {
          this.enviando = false;
          this.toast.error(
            extractErrorMessage(
              error,
              'El código no es válido o ya expiró. Solicita uno nuevo.',
            ),
          );
        },
      });
  }

  private validarContrasena(): string {
    if (this.contrasena.length < 8)
      return 'La contraseña debe tener al menos 8 caracteres.';
    if (!/[A-Z]/.test(this.contrasena))
      return 'Incluye al menos una letra mayúscula.';
    if (!/[0-9]/.test(this.contrasena)) return 'Incluye al menos un número.';
    if (!/[^a-zA-Z0-9]/.test(this.contrasena))
      return 'Incluye al menos un carácter especial.';
    if (this.contrasena !== this.repetirContrasena)
      return 'Las contraseñas no coinciden.';
    return '';
  }
}
