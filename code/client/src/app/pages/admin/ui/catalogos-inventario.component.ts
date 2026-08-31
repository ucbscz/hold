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
import { UsuarioServiceAPI } from '@entities/user';
import { BuscadorComponent } from '@features/admin-search';
import {
  Tabla,
  TablePaginationComponent,
  printTable,
} from '@shared/lib/admin-table';
import { AuditPanelComponent } from '@widgets/audit-panel';
import {
  AvisoEliminarComponent,
  CustomSelectComponent,
  OpcionSelect,
} from '@shared/ui';
import { extractErrorMessage } from '@shared/lib/error';
import { Subscription, finalize } from 'rxjs';

@Component({
  selector: 'app-catalogos-inventario',
  standalone: true,
  imports: [
    CommonModule,
    ValidatedFormsModule,
    BuscadorComponent,
    TablePaginationComponent,
    AvisoEliminarComponent,
    AuditPanelComponent,
    CustomSelectComponent,
  ],
  template: `
    <header class="catalog-heading header-title">
      <h1>{{ titulo }}</h1>
      <p class="subtitle">
        {{
          tipo === 'ambientes'
            ? 'Ambientes y responsables del laboratorio'
            : 'Origen de los equipos del inventario'
        }}
      </p>
    </header>
    @if (activeTab === 'tabla') {
      <app-buscador (terminoBusqueda)="aplicarFiltros($event)">
        <button type="button" class="botoncrear" (click)="abrirEditor()">
          <i class="fas fa-plus"></i
          >{{ tipo === 'ambientes' ? 'Nuevo ambiente' : 'Nueva procedencia' }}
        </button>
        <button
          type="button"
          class="btn admin-toolbar-button"
          (click)="exportarCsv(tipo, encabezados, filasExportadas)"
        >
          <i class="fas fa-file-csv"></i>Exportar
        </button>
        <button
          type="button"
          class="btn admin-toolbar-button admin-toolbar-button--icon"
          aria-label="Imprimir tabla"
          title="Imprimir tabla"
          (click)="imprimir()"
        >
          <i class="fas fa-print"></i>
        </button>
      </app-buscador>
      <ng-container [ngTemplateOutlet]="tabs" />
      @if (mensajeError) {
        <p class="catalog-error" role="alert">{{ mensajeError }}</p>
      }
      <div
        class="table-responsive"
        [class.table-responsive--compact]="tipo === 'procedencias'"
      >
        <table
          class="data-table data-table--catalog"
          [class.data-table--compact]="tipo === 'procedencias'"
        >
          <thead>
            <tr>
              @for (columna of encabezados; track columna) {
                <th
                  scope="col"
                  [attr.aria-sort]="
                    tipo === 'ambientes' ? ariaOrdenColumna(columna) : null
                  "
                >
                  @if (tipo === 'ambientes') {
                    <button
                      type="button"
                      class="table-sort-button"
                      (click)="ordenarPorColumna(columna)"
                    >
                      {{ columna
                      }}<i
                        class="fas"
                        [ngClass]="iconoOrdenColumna(columna)"
                        aria-hidden="true"
                      ></i>
                    </button>
                  } @else {
                    {{ columna }}
                  }
                </th>
              }
              <th scope="col" class="actions-column">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (item of paginar(filtrados); track item.Id) {
              <tr>
                <td>
                  <span class="table-cell-label" [title]="item.Nombre">{{
                    item.Nombre
                  }}</span>
                </td>
                @if (tipo === 'ambientes') {
                  <td>
                    <span
                      class="table-cell-label"
                      [title]="
                        item.NombreAdministrador || 'Sin responsable asignado'
                      "
                      >{{
                        item.NombreAdministrador || 'Sin responsable asignado'
                      }}</span
                    >
                  </td>
                }
                <td class="actions-column">
                  <button
                    type="button"
                    class="btn-icon btn-edit"
                    title="Editar"
                    aria-label="Editar"
                    (click)="abrirEditor(item)"
                  >
                    <i class="fas fa-pencil-alt"></i>
                  </button>
                  <button
                    type="button"
                    class="btn-icon btn-delete"
                    title="Eliminar"
                    aria-label="Eliminar"
                    (click)="eliminando = item.Id"
                  >
                    <i class="fas fa-trash-alt"></i>
                  </button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td [attr.colspan]="encabezados.length + 1" class="empty-table">
                  {{
                    cargando
                      ? 'Cargando registros...'
                      : 'Sin registros coincidentes'
                  }}
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <app-table-pagination
        [totalItems]="filtrados.length"
        [page]="paginaActual"
        [pageSize]="filasPorPagina"
        (pageChange)="cambiarPagina($event)"
      />
    } @else {
      <app-audit-panel
        [entidad]="tipo === 'ambientes' ? 'Ambiente' : 'Procedencia'"
        [refreshTrigger]="auditRefresh"
      >
        <div admin-tabs><ng-container [ngTemplateOutlet]="tabs" /></div>
      </app-audit-panel>
    }
    <ng-template #tabs
      ><nav class="admin-tabs" aria-label="Vistas del catálogo">
        <button
          type="button"
          class="admin-tabs__button"
          [class.admin-tabs__button--active]="activeTab === 'tabla'"
          (click)="seleccionarTab('tabla')"
        >
          Tabla
        </button>
        <button
          type="button"
          class="admin-tabs__button"
          [class.admin-tabs__button--active]="activeTab === 'auditoria'"
          (click)="seleccionarTab('auditoria')"
        >
          Auditoría
        </button>
      </nav></ng-template
    >
    <dialog
      #editor
      aria-labelledby="catalog-editor-title"
      (cancel)="ocupado && $event.preventDefault()"
    >
      <header>
        <h2 id="catalog-editor-title">
          {{ editando ? 'Editar' : 'Registrar' }}
          {{ tipo === 'ambientes' ? 'ambiente' : 'procedencia' }}
        </h2>
        <button
          type="button"
          class="modal-close-btn"
          aria-label="Cerrar"
          [disabled]="ocupado"
          (click)="editor.close()"
        >
          <i class="fas fa-times"></i>
        </button>
      </header>
      <form #form="ngForm" (ngSubmit)="guardar()">
        <label for="catalog-name">Nombre</label>
        <input
          id="catalog-name"
          name="nombre"
          [(ngModel)]="nombre"
          maxlength="255"
          required
          [disabled]="ocupado"
        />
        @if (tipo === 'ambientes') {
          <label for="catalog-admin">Administrador de laboratorio</label>
          <app-custom-select
            id="catalog-admin"
            name="responsable"
            [(ngModel)]="carnetAdministrador"
            [opciones]="administradores"
            placeholder="Sin responsable asignado"
            [disabled]="ocupado"
          />
        }
        @if (errorFormulario) {
          <p class="catalog-error" role="alert">{{ errorFormulario }}</p>
        }
        <footer>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="editor.close()"
            [disabled]="ocupado"
          >
            Cancelar</button
          ><button
            type="submit"
            class="btn btn-primary"
            [disabled]="form.invalid || !nombre.trim() || ocupado"
          >
            {{ ocupado ? 'Guardando...' : 'Guardar' }}
          </button>
        </footer>
      </form>
    </dialog>
    @if (eliminando) {
      <app-aviso-eliminar
        mensaje="¿Eliminar este registro?"
        (aceptar)="eliminar()"
        (cancelar)="eliminando = undefined"
      />
    }
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
      font-family: var(--font);
      color: var(--ink);
      padding-block: 1.5rem;
    }
    .catalog-heading {
      margin-bottom: 1.5rem;
    }
    h1 {
      margin: 0;
      font-size: 1.75rem;
    }
    .catalog-heading p {
      margin: 0.5rem 0 0;
      font-size: 0.875rem;
      color: var(--ink-secondary);
    }
    .table-responsive {
      overflow-x: auto;
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      background: var(--surface);
    }
    table {
      width: 100%;
      border-collapse: collapse;
      table-layout: fixed;
    }
    th,
    td {
      padding: 1rem;
      text-align: left;
      border-bottom: 1px solid var(--border);
      overflow-wrap: anywhere;
    }
    th {
      background: var(--sidebar);
    }
    .actions-column {
      width: 7rem;
      white-space: nowrap;
      text-align: center;
    }
    .empty-table {
      text-align: center;
      color: var(--ink-secondary);
      padding: 2rem 1rem;
    }
    .catalog-error {
      color: var(--error);
      overflow-wrap: anywhere;
    }
    dialog {
      box-sizing: border-box;
      width: min(520px, calc(100vw - 24px));
      max-height: calc(100dvh - 24px);
      overflow: auto;
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      padding: 0;
      color: var(--ink);
      background: var(--surface);
      font-family: var(--font);
    }
    dialog::backdrop {
      background: rgba(0, 0, 0, 0.45);
    }
    dialog header {
      padding: 24px 64px 16px 24px;
      position: relative;
    }
    h2 {
      margin: 0;
      font-size: 20px;
      overflow-wrap: anywhere;
    }
    .modal-close-btn {
      position: absolute;
      right: 16px;
      top: 16px;
    }
    form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 0 24px 24px;
    }
    input {
      min-width: 0;
      box-sizing: border-box;
      width: 100%;
      height: 44px;
      border: 1px solid var(--border);
      border-radius: var(--radius-full);
      padding-inline: 1rem;
      font: inherit;
      color: var(--ink);
      background: var(--surface);
    }
    input:focus-visible {
      outline: 2px solid var(--interactive-text);
      outline-offset: 2px;
    }
    footer {
      display: flex;
      justify-content: flex-end;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin-top: 12px;
    }
    @media (max-width: 600px) {
      table {
        min-width: 480px;
      }
      input {
        font-size: 16px;
      }
    }
  `,
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
                  u.rol?.toLowerCase() === 'administrador_laboratorio' &&
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
