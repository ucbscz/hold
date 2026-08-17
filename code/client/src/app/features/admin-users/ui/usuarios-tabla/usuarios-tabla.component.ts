import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Carrera } from '@entities/admin';
import { CarreraService } from '@entities/career';
import { Usuario, UsuarioServiceAPI } from '@entities/user';
import { BuscadorComponent } from '@features/admin-search';
import { Tabla, TablePaginationComponent } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoEliminarComponent,
  AvisoExitoComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { PrestamosInlineComponent } from '@widgets/admin-inline';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { UsuariosCrearComponent } from '../usuarios-crear/usuarios-crear.component';
import { UsuariosEditarComponent } from '../usuarios-editar/usuarios-editar.component';
@Component({
  selector: 'app-usuarios-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    UsuariosCrearComponent,
    UsuariosEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    PrestamosInlineComponent,
    AuditPanelComponent,
    TablePaginationComponent,
  ],
  templateUrl: './usuarios-tabla.component.html',
  styleUrls: ['./usuarios-tabla.component.css'],
})
export class UsuariosTablaComponent extends Tabla implements OnInit {
  expandedRowId: number | null = null;
  auditRefresh = 0;
  expandedCarnet: string | null = null;

  toggleExpandCarnet(carnet: string) {
    this.expandedCarnet = this.expandedCarnet === carnet ? null : carnet;
  }

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }
  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  valoreliminar: number = 0;
  bloqueoModalVisible = false;
  usuarioBloqueo: Usuario | null = null;
  motivoBloqueo = '';
  usuarios: Usuario[] = [];
  usuarioscopia: Usuario[] = [];
  carreras: string[] = [];
  usuarioSeleccionado: Usuario = new Usuario();
  override columnas: string[] = [
    'Carnet',
    'Nombre',
    'Apellido Paterno',
    'Apellido Materno',
    'Correo',
    'Teléfono',
    'Rol',
    'Carrera',
    'Referencia',
    'Tel. Referencia',
  ];
  constructor(
    private readonly usuarioapi: UsuarioServiceAPI,
    private carrerasAPI: CarreraService,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarUsuarios();
    this.cargarCarreras();
  }
  alternarBloqueo(usuario: Usuario) {
    if (usuario.bloqueado) {
      this.aplicarBloqueo(usuario, false, null);
      return;
    }

    this.usuarioBloqueo = usuario;
    this.motivoBloqueo = '';
    this.bloqueoModalVisible = true;
  }

  confirmarBloqueo() {
    if (this.usuarioBloqueo) {
      this.aplicarBloqueo(this.usuarioBloqueo, true, this.motivoBloqueo);
    }
    this.cerrarBloqueo();
  }

  cerrarBloqueo() {
    this.bloqueoModalVisible = false;
    this.usuarioBloqueo = null;
    this.motivoBloqueo = '';
  }

  private aplicarBloqueo(
    usuario: Usuario,
    bloqueado: boolean,
    motivo: string | null,
  ) {
    this.usuarioapi
      .bloquearUsuario(usuario.carnet!, bloqueado, motivo)
      .subscribe({
        next: () => {
          usuario.bloqueado = bloqueado;
          usuario.motivo_bloqueo = bloqueado ? motivo : null;
          this.mensajeexito = bloqueado
            ? 'Usuario bloqueado'
            : 'Usuario desbloqueado';
          this.exito.set(true);
          this.auditRefresh++;
        },
        error: (error) => {
          this.mensajeerror = extractErrorMessage(
            error,
            'Error al cambiar el bloqueo del usuario',
          );
          this.error.set(true);
        },
      });
  }

  crearusuario() {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }
  cargarCarreras() {
    this.carrerasAPI.obtenerCarreras().subscribe({
      next: (data: Carrera[]) => {
        this.carreras = data.map((carrera) => carrera.Nombre ?? '');
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar las carreras, intente más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  cargarUsuarios() {
    this.usuarioapi.obtenerUsuarios().subscribe({
      next: (data: Usuario[]) => {
        this.usuarios = data;
        this.usuarioscopia = [...this.usuarios];
        this.aplicarOrdenActualSiExiste();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los usuarios, intente más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  actualizarTabla() {
    this.cargarUsuarios();
  }
  aplicarFiltros(event?: [string, string]) {
    if (event && event[0].trim() !== '') {
      const busquedaNormalizada = this.normalizeText(event[0]);
      this.usuarios = this.usuarioscopia.filter((usuario) => {
        switch (event[1]) {
          case 'Carnet':
            return this.normalizeText(usuario.carnet || '').includes(
              busquedaNormalizada,
            );
          case 'Nombre':
            return this.normalizeText(usuario.nombre || '').includes(
              busquedaNormalizada,
            );
          case 'Apellido Paterno':
            return this.normalizeText(usuario.apellido_paterno || '').includes(
              busquedaNormalizada,
            );
          case 'Apellido Materno':
            return this.normalizeText(usuario.apellido_materno || '').includes(
              busquedaNormalizada,
            );
          case 'Correo':
            return this.normalizeText(usuario.correo || '').includes(
              busquedaNormalizada,
            );
          case 'Teléfono':
            return this.normalizeText(usuario.telefono || '').includes(
              busquedaNormalizada,
            );
          case 'Rol':
            return this.normalizeText(usuario.rol || '').includes(
              busquedaNormalizada,
            );
          case 'Carrera':
            return this.normalizeText(usuario.carrera || '').includes(
              busquedaNormalizada,
            );
          case 'Referencia':
            return this.normalizeText(usuario.nombre_referencia || '').includes(
              busquedaNormalizada,
            );
          case 'Tel. Referencia':
            return this.normalizeText(
              usuario.telefono_referencia || '',
            ).includes(busquedaNormalizada);
          default:
            return (
              this.normalizeText(usuario.carnet || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.nombre || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.apellido_paterno || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.apellido_materno || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.correo || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.telefono || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.rol || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.carrera || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.nombre_referencia || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(usuario.telefono_referencia || '').includes(
                busquedaNormalizada,
              )
            );
        }
      });
    } else {
      this.usuarios = [...this.usuarioscopia];
    }
    this.reiniciarPaginacion();
    this.aplicarOrdenActualSiExiste();
  }
  limpiarBusqueda() {
    this.usuarios = [...this.usuarioscopia];
    this.aplicarOrdenActualSiExiste();
  }
  editarUsuario(usuario: Usuario) {
    this.botoncrear.set(false);
    this.usuarioSeleccionado = usuario;
    this.botoneditar.set(true);
  }
  eliminarUsuario(i: number) {
    this.valoreliminar = i;
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    const usuarioAEliminar = this.usuarios[this.valoreliminar];
    this.usuarioapi.eliminarUsuario(usuarioAEliminar.id || '').subscribe({
      next: (_response) => {
        this.mensajeexito = 'Usuario eliminado exitosamente.';
        this.exito.set(true);
        this.auditRefresh++;
        this.usuarios.splice(this.valoreliminar, 1);
        this.usuarioscopia = [...this.usuarios];
        this.alertaeliminar = false;
        this.valoreliminar = 0;
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al eliminar el usuario, intente más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
        this.alertaeliminar = false;
        this.valoreliminar = 0;
      },
    });
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.valoreliminar = 0;
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.usuarios = this.sortByColumn(this.usuarios, e, {
      Carnet: (usuario) => usuario.carnet,
      Nombre: (usuario) => usuario.nombre,
      'Apellido Paterno': (usuario) => usuario.apellido_paterno,
      'Apellido Materno': (usuario) => usuario.apellido_materno,
      Correo: (usuario) => usuario.correo,
      Teléfono: (usuario) => usuario.telefono,
      Rol: (usuario) => usuario.rol,
      Carrera: (usuario) => usuario.carrera,
      Referencia: (usuario) => usuario.nombre_referencia,
      'Tel. Referencia': (usuario) => usuario.telefono_referencia,
    });
  }
}
