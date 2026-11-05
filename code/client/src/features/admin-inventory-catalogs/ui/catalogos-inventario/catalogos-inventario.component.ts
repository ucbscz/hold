import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import {
  CatalogoInventario,
  CatalogoInventarioService,
  TipoCatalogo,
} from '@entities/equipment';
import {
  UsuarioServiceAPI,
  ROL_ADMINISTRADOR_LABORATORIO,
} from '@entities/user';
import {
  AvisoEliminarComponent,
  BuscadorComponent,
  CustomSelectComponent,
  OpcionSelect,
} from '@shared/ui';
import {
  AdminTabsComponent,
  Tabla,
  TablePaginationComponent,
  printTable,
} from '@shared/lib/admin-table';
import { AuditPanelComponent } from '@entities/audit-log';
import { extractErrorMessage } from '@shared/lib/error';
import { Subscription, finalize } from 'rxjs';

@Component({
  selector: 'app-catalogos-inventario',
  standalone: true,
  imports: [
    AdminTabsComponent,
    CommonModule,
    ValidatedFormsModule,
    BuscadorComponent,
    TablePaginationComponent,
    AvisoEliminarComponent,
    AuditPanelComponent,
    CustomSelectComponent,
  ],
  templateUrl: './catalogos-inventario.component.html',
  styleUrl: './catalogos-inventario.component.css',
})
export class CatalogosInventarioComponent
  extends Tabla
  implements OnChanges, OnDestroy
{
  @Input() tipo: TipoCatalogo = 'ambientes';
  @ViewChild('editor', { static: true }) editor!: ElementRef<HTMLDialogElement>;
  override sortColumn = 'Nombre';
  items: CatalogoInventario[] = [];
  busqueda = '';
  nombre = '';
  carnetAdministrador: string | null = null;
  administradores: OpcionSelect[] = [
    { value: '', label: 'Sin responsable asignado' },
  ];
  mensajeError = '';
  errorFormulario = '';
  cargando = false;
  ocupado = false;
  auditRefresh = 0;
  editando?: number;
  eliminando?: number;
  private carga?: Subscription;
  private responsables?: Subscription;
  constructor(
    private readonly api: CatalogoInventarioService,
    private readonly usuarios: UsuarioServiceAPI,
  ) {
    super();
  }
  get titulo() {
    return this.tipo === 'ambientes' ? 'Ambientes' : 'Procedencias';
  }
  get encabezados() {
    return this.tipo === 'ambientes'
      ? ['Nombre', 'Administrador de laboratorio']
      : ['Nombre'];
  }
  ngOnChanges() {
    this.nombre = '';
    this.busqueda = '';
    this.editando = undefined;
    this.activeTab = 'tabla';
    this.sortColumn = 'Nombre';
    this.sortDirection = 'asc';
    this.reiniciarPaginacion();
    this.editor?.nativeElement.close();
    this.cargar();
    this.responsables?.unsubscribe();
    if (this.tipo === 'ambientes')
      this.responsables = this.usuarios.obtenerUsuarios().subscribe({
        next: (users) =>
          (this.administradores = [
            { value: '', label: 'Sin responsable asignado' },
            ...users
              .filter(
                (u) =>
                  u.rol?.toLowerCase() === ROL_ADMINISTRADOR_LABORATORIO &&
                  !u.bloqueado,
              )
              .map((u) => ({
                value: u.carnet!,
                label: [u.nombre, u.apellido_paterno, u.apellido_materno]
                  .filter(Boolean)
                  .join(' '),
              })),
          ]),
        error: (e) => (this.mensajeError = extractErrorMessage(e)),
      });
  }
  ngOnDestroy() {
    this.carga?.unsubscribe();
    this.responsables?.unsubscribe();
  }
  override aplicarFiltros(event?: [string, string]) {
    this.busqueda = event?.[0] ?? this.busqueda;
    this.reiniciarPaginacion();
  }
  override sortTable(sort: { col: string; dir: 'asc' | 'desc' }) {
    this.sortColumn = sort.col;
    this.sortDirection = sort.dir;
  }
  get filtrados() {
    return this.sortByColumn(
      this.items.filter((i) =>
        this.normalizeText(
          i.Nombre + ' ' + (i.NombreAdministrador ?? ''),
        ).includes(this.normalizeText(this.busqueda)),
      ),
      { col: this.sortColumn, dir: this.sortDirection },
      {
        Nombre: (i) => i.Nombre,
        'Administrador de laboratorio': (i) => i.NombreAdministrador,
      },
    );
  }
  get filasExportadas() {
    return this.filtrados.map((i) =>
      this.tipo === 'ambientes'
        ? [i.Nombre, i.NombreAdministrador || 'Sin responsable asignado']
        : [i.Nombre],
    );
  }
  imprimir() {
    printTable({
      title: this.titulo,
      headers: this.encabezados,
      rows: this.filasExportadas,
    });
  }
  abrirEditor(item?: CatalogoInventario) {
    this.editando = item?.Id;
    this.nombre = item?.Nombre ?? '';
    this.carnetAdministrador = item?.CarnetAdministrador ?? null;
    this.errorFormulario = '';
    this.editor.nativeElement.showModal();
  }
  cargar() {
    this.carga?.unsubscribe();
    this.cargando = true;
    this.carga = this.api
      .listar(this.tipo)
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: (items) => {
          this.items = items;
          this.paginaActual = Math.min(
            this.paginaActual,
            Math.max(1, Math.ceil(this.filtrados.length / this.filasPorPagina)),
          );
          this.mensajeError = '';
        },
        error: (e) => {
          this.items = [];
          this.mensajeError = extractErrorMessage(e);
        },
      });
  }
  guardar() {
    if (this.ocupado || !this.nombre.trim()) return;
    this.ocupado = true;
    this.api
      .guardar(this.tipo, this.nombre, this.editando, this.carnetAdministrador)
      .pipe(finalize(() => (this.ocupado = false)))
      .subscribe({
        next: () => {
          this.editor.nativeElement.close();
          this.auditRefresh++;
          this.cargar();
        },
        error: (e) => (this.errorFormulario = extractErrorMessage(e)),
      });
  }
  eliminar() {
    if (!this.eliminando || this.ocupado) return;
    this.ocupado = true;
    this.api
      .eliminar(this.tipo, this.eliminando)
      .pipe(finalize(() => (this.ocupado = false)))
      .subscribe({
        next: () => {
          this.eliminando = undefined;
          this.auditRefresh++;
          this.cargar();
        },
        error: (e) => {
          this.eliminando = undefined;
          this.mensajeError = extractErrorMessage(e);
        },
      });
  }
}
