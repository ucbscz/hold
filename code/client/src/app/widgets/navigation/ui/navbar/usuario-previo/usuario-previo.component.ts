import { Component, Input, signal, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { UsuarioService } from '@entities/user';
import { AuthService } from '@features/auth-session';
@Component({
  selector: 'app-usuario-previo',
  imports: [],
  templateUrl: './usuario-previo.component.html',
  styleUrl: './usuario-previo.component.css',
})
export class UsuarioPrevioComponent {
  sesion: boolean;
  rol: string;
  isInAdminMode: boolean = false;
  @Input() showUserMenu: WritableSignal<Boolean> = signal(true);
  constructor(
    private readonly router: Router,
    private usuario: UsuarioService,
    private authService: AuthService,
  ) {
    this.sesion = !usuario.estaVacio();
    this.rol = usuario.obtenerRol();
    this.isInAdminMode = this.router.url.includes('/admin');
  }
  seleccionar(item: string) {
    if (this.usuario.estaVacio() == true) {
      this.router.navigate(['/login']);
    } else if (item == 'iniciar-sesion') {
      this.router.navigate(['/login']);
    } else if (item == 'perfil') {
      this.router.navigate(['/profile']);
    } else if (item == 'historial') {
      this.router.navigate(['/loan-history']);
    } else if (item == 'cerrar-sesion') {
      this.authService.clear();
      this.usuario.limpiarSesion();
      this.router.navigate(['/login']);
    } else if (item == 'admin') {
      this.router.navigate(['/admin']);
    } else if (item == 'modousuario') {
      this.router.navigate(['/home']);
    }
    this.showUserMenu.set(false);
  }
}
