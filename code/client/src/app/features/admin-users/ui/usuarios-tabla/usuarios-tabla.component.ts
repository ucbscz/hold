import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
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
    UsuariosCrearComponent,
    UsuariosEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    PrestamosInlineComponent,
    AuditPanelComponent,
    TablePaginationComponent,
    CustomSelectComponent,
  ],
  templateUrl: './usuarios-tabla.component.html',
  styleUrls: ['./usuarios-tabla.component.css'],
})
export class UsuariosTablaComponent extends Tabla implements OnInit {
  auditRefresh = 0;
  expandedCarnet: string | null = null;

  toggleExpandCarnet(carnet: string) {
    this.expandedCarnet = this.expandedCarnet === carnet ? null : carnet;
  }

  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  usuarioAEliminar: Usuario | null = null;
  bloqueoModalVisible = false;
  usuarioBloqueo: Usuario | null = null;
  motivoBloqueo = '';
  procesandoBloqueo = false;
  usuarios: Usuario[] = [];
  usuarioscopia: Usuario[] = [];
  carreras: string[] = [];
  filtroRol = '';
  filtroCarrera = '';
  filtroBloqueo = '';
  filtroBusqueda: [string, string] = ['', ''];
  readonly rolesFiltroOpciones: OpcionSelect[] = [
    { value: '', label: 'Todos los roles' },
    { value: 'administrador', label: 'Administrador' },
    { value: 'estudiante', label: 'Estudiante' },
  ];
  carrerasFiltroOpciones: OpcionSelect[] = [
    { value: '', label: 'Todas las carreras' },
  ];
  readonly bloqueoFiltroOpciones: OpcionSelect[] = [
    { value: '', label: 'Todos los accesos' },
    { value: 'bloqueados', label: 'Bloqueados' },
    { value: 'habilitados', label: 'Habilitados' },
  ];
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
    this.usuarioBloqueo = usuario;
    this.motivoBloqueo = '';
    this.bloqueoModalVisible = true;
  }

  confirmarBloqueo() {
    if (!this.usuarioBloqueo || this.procesandoBloqueo) return;

    const bloquear = !this.usuarioBloqueo.bloqueado;
    const motivo = bloquear ? this.motivoBloqueo.trim() : null;
    if (bloquear && !motivo) return;

    this.aplicarBloqueo(this.usuarioBloqueo, bloquear, motivo);
  }

  cerrarBloqueo() {
    if (this.procesandoBloqueo) return;
    this.bloqueoModalVisible = false;
    this.usuarioBloqueo = null;
    this.motivoBloqueo = '';
  }

  private aplicarBloqueo(
    usuario: Usuario,
    bloqueado: boolean,
    motivo: string | null,
  ) {
    this.procesandoBloqueo = true;
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
          this.procesandoBloqueo = false;
          this.bloqueoModalVisible = false;
          this.usuarioBloqueo = null;
          this.motivoBloqueo = '';
          this.cargarUsuarios();
        },
        error: (error) => {
          this.procesandoBloqueo = false;
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
        this.carrerasFiltroOpciones = [
          { value: '', label: 'Todas las carreras' },
          ...this.carreras
            .filter(Boolean)
            .map((carrera) => ({ value: carrera, label: carrera })),
        ];
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
        this.aplicarFiltros();
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
    if (event) this.filtroBusqueda = event;

    const busquedaNormalizada = this.normalizeText(this.filtroBusqueda[0]);
    const columna = this.filtroBusqueda[1];
    const rol = this.normalizeText(this.filtroRol);
    const carrera = this.normalizeText(this.filtroCarrera);
    const bloqueo = this.filtroBloqueo;

    this.usuarios = this.usuarioscopia.filter((usuario) => {
      const coincideBusqueda = !busquedaNormalizada
        ? true
        : (() => {
            switch (columna) {
              case 'Carnet':
                return this.normalizeText(usuario.carnet || '').includes(
                  busquedaNormalizada,
                );
              case 'Nombre':
                return this.normalizeText(usuario.nombre || '').includes(
                  busquedaNormalizada,
                );
              case 'Apellido Paterno':
                return this.normalizeText(
                  usuario.apellido_paterno || '',
                ).includes(busquedaNormalizada);
              case 'Apellido Materno':
                return this.normalizeText(
                  usuario.apellido_materno || '',
                ).includes(busquedaNormalizada);
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
                return this.normalizeText(
                  usuario.nombre_referencia || '',
                ).includes(busquedaNormalizada);
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
                  this.normalizeText(
                    usuario.telefono_referencia || '',
                  ).includes(busquedaNormalizada)
                );
            }
          })();
      const coincideRol = !rol || this.normalizeText(usuario.rol) === rol;
      const coincideCarrera =
        !carrera || this.normalizeText(usuario.carrera) === carrera;
      const coincideBloqueo =
        !bloqueo ||
        (bloqueo === 'bloqueados' && usuario.bloqueado === true) ||
        (bloqueo === 'habilitados' && usuario.bloqueado !== true);

      return (
        coincideBusqueda && coincideRol && coincideCarrera && coincideBloqueo
      );
    });
    this.reiniciarPaginacion();
    this.aplicarOrdenActualSiExiste();
  }

  seleccionarRol(valor: unknown): void {
    this.filtroRol = String(valor ?? '');
    this.aplicarFiltros();
  }

  seleccionarCarrera(valor: unknown): void {
    this.filtroCarrera = String(valor ?? '');
    this.aplicarFiltros();
  }

  seleccionarBloqueo(valor: unknown): void {
    this.filtroBloqueo = String(valor ?? '');
    this.aplicarFiltros();
  }

  get desbloqueandoUsuario(): boolean {
    return Boolean(this.usuarioBloqueo?.bloqueado);
  }

  get puedeConfirmarBloqueo(): boolean {
    return this.desbloqueandoUsuario || this.motivoBloqueo.trim().length > 0;
  }
  editarUsuario(usuario: Usuario) {
    this.botoncrear.set(false);
    this.usuarioSeleccionado = usuario;
    this.botoneditar.set(true);
  }
  eliminarUsuario(usuario: Usuario) {
    this.usuarioAEliminar = usuario;
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    if (!this.usuarioAEliminar) return;

    this.usuarioapi.eliminarUsuario(this.usuarioAEliminar.id || '').subscribe({
      next: () => {
        this.mensajeexito = 'Usuario eliminado exitosamente.';
        this.exito.set(true);
        this.auditRefresh++;
        this.alertaeliminar = false;
        this.usuarioAEliminar = null;
        this.cargarUsuarios();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al eliminar el usuario, intente más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
        this.alertaeliminar = false;
        this.usuarioAEliminar = null;
      },
    });
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.usuarioAEliminar = null;
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
