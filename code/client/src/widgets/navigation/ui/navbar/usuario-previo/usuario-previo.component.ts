import { Component, Input, signal, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, UsuarioService } from '@entities/user';
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
    this.isInAdminMode = this.router.url.includes('/administracion');
  }
  seleccionar(item: string) {
    if (this.usuario.estaVacio() == true) {
      this.router.navigate(['/login']);
    } else if (item == 'iniciar-sesion') {
      this.router.navigate(['/login']);
    } else if (item == 'perfil') {
      this.router.navigate(['/perfil']);
    } else if (item == 'historial') {
      this.router.navigate(['/historial']);
    } else if (item == 'cerrar-sesion') {
      this.usuario.limpiarSesion();
      this.authService.logout().subscribe({
        next: () => void this.router.navigate(['/login']),
        error: () => void this.router.navigate(['/login']),
      });
    } else if (item == 'admin') {
      this.router.navigate(['/administracion']);
    } else if (item == 'modousuario') {
      this.router.navigate(['/inicio']);
    }
    this.showUserMenu.set(false);
  }
}
