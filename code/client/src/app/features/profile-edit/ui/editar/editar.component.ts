import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Carrera } from '@entities/admin';
import { CarreraService } from '@entities/career';
import { Usuario, UsuarioService, UsuarioServiceAPI } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  PantallaCargaComponent,
} from '@shared/ui';
import { finalize } from 'rxjs';

const CLOSE_SUCCESS_DELAY_MS = 2000;

@Component({
  selector: 'app-editar',
  imports: [
    CommonModule,
    FormsModule,
    AvisoExitoComponent,
    MostrarerrorComponent,
    PantallaCargaComponent,
    CustomSelectComponent,
  ],
  templateUrl: './editar.component.html',
  styleUrl: './editar.component.css',
})
export class EditarComponent {
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Input() usuario: Usuario = new Usuario();
  @Output() guardado = new EventEmitter<Usuario>();
  localUsuario: Usuario = new Usuario();
  exito: WritableSignal<boolean> = signal(false);
  mensajeexito: string = '';
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = '';
  carreras: string[] = [];
  contrasena: string = '';
  cargando: boolean = false;

  constructor(
    private readonly usuarioApi: UsuarioServiceAPI,
    private readonly carrerasAPI: CarreraService,
    private readonly usuarioStore: UsuarioService,
  ) {}
  ngOnInit() {
    this.localUsuario = { ...this.usuario };
    this.cargarcarrera();
  }
  cargarcarrera() {
    this.carrerasAPI.obtenerCarreras().subscribe({
      next: (data: Carrera[]) => {
        this.carreras = data.map((carrera) => carrera.Nombre ?? '');
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al obtener las carreras, intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  confirmar() {
    this.cargando = true;
    this.usuarioApi
      .editarUsuario(this.localUsuario, this.contrasena)
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: () => {
          this.usuarioStore.actualizarUsuario({ ...this.localUsuario });
          this.guardado.emit({ ...this.localUsuario });
          this.mensajeexito = 'Perfil actualizado correctamente';
          this.exito.set(true);
          setTimeout(() => this.cerrar(), CLOSE_SUCCESS_DELAY_MS);
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al actualizar el usuario, intente mas tarde',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
  }
  cerrar() {
    this.botoneditar.set(false);
  }
}
