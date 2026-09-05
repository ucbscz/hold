import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UsuarioServiceAPI } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';

@Component({
  selector: 'app-recuperar-contrasena',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recuperar-contrasena.component.html',
  styleUrl: './recuperar-contrasena.component.css',
})
export class RecuperarContrasenaComponent {
  readonly token: string | null;
  email = '';
  contrasena = '';
  repetirContrasena = '';
  enviando = false;
  completado = false;
  mensaje = '';
  error = '';
  mostrarContrasena = false;
  mostrarRepetirContrasena = false;

  constructor(
    route: ActivatedRoute,
    private readonly users: UsuarioServiceAPI,
  ) {
    this.token = route.snapshot.queryParamMap.get('token');
  }

  solicitar(): void {
    if (this.enviando || !this.email) return;

    this.enviando = true;
    this.error = '';
    this.users.solicitarRecuperacionContrasena(this.email).subscribe({
      next: () => {
        this.completado = true;
        this.enviando = false;
        this.mensaje =
          'Si existe una cuenta local verificada con este correo, recibirás un enlace para restablecer tu contraseña.';
      },
      error: () => {
        this.enviando = false;
        this.error = 'No se pudo procesar la solicitud. Intenta nuevamente.';
      },
    });
  }

  restablecer(): void {
    if (this.enviando || !this.token) return;

    this.error = this.validarContrasena();
    if (this.error) return;

    this.enviando = true;
    this.users.restablecerContrasena(this.token, this.contrasena).subscribe({
      next: () => {
        this.completado = true;
        this.enviando = false;
        this.mensaje =
          'Tu contraseña fue actualizada. Ya puedes iniciar sesión con ella.';
      },
      error: (error) => {
        this.enviando = false;
        this.error = extractErrorMessage(
          error,
          'No se pudo restablecer la contraseña. Solicita un nuevo enlace.',
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
