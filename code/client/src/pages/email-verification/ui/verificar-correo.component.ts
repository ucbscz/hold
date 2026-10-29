import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UsuarioServiceAPI } from '@entities/user';

@Component({
  selector: 'app-verificar-correo',
  imports: [RouterLink],
  templateUrl: './verificar-correo.component.html',
  styleUrl: './verificar-correo.component.css',
})
export class VerificarCorreoComponent implements OnInit {
  estado: 'cargando' | 'verificado' | 'error' = 'cargando';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly users: UsuarioServiceAPI,
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.estado = 'error';
      return;
    }
    this.users.verificarCorreo(token).subscribe({
      next: () => (this.estado = 'verificado'),
      error: () => (this.estado = 'error'),
    });
  }
}
