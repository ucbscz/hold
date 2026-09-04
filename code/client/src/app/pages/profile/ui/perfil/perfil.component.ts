import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Usuario, UsuarioService, UsuarioServiceAPI } from '@entities/user';
import { EditarComponent } from '@features/profile-edit';
@Component({
  selector: 'app-perfil',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, EditarComponent],
  templateUrl: './perfil.component.html',
  styleUrls: ['./perfil.component.css'],
})
export class PerfilComponent implements OnInit {
  editar: WritableSignal<boolean> = signal(false);
  usuario: Usuario = new Usuario();
  constructor(
    private readonly usuarioS: UsuarioService,
    private readonly usuarioApi: UsuarioServiceAPI,
  ) {
    this.usuario = this.usuarioS.obtenerUsuario();
  }
  ngOnInit(): void {
    this.usuarioApi.obtenerPerfil().subscribe({
      next: (usuario) => {
        this.usuario = usuario;
        this.usuarioS.actualizarUsuario(usuario);
      },
    });
  }
  toggleEdit() {
    this.editar.set(!this.editar());
  }
  onGuardado(actualizado: Usuario) {
    this.usuario = { ...actualizado };
  }

  imagenSrc(imagen: string | null | undefined): string | null {
    if (!imagen) return null;
    return imagen.startsWith('data:')
      ? imagen
      : `data:image/jpeg;base64,${imagen}`;
  }
}
