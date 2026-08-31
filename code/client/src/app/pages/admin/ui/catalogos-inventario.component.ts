import { Component, Input, OnChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CatalogoInventario,
  CatalogoInventarioService,
  TipoCatalogo,
} from '@entities/equipment';
import {
  Tabla,
  TablePaginationComponent,
  printTable,
} from '@shared/lib/admin-table';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { AvisoEliminarComponent } from '@shared/ui';
import { extractErrorMessage } from '@shared/lib/error';

@Component({
  selector: 'app-catalogos-inventario',
  standalone: true,
  imports: [
    FormsModule,
    TablePaginationComponent,
    AvisoEliminarComponent,
    AuditPanelComponent,
  ],
  template: `
    <h1>{{ tipo === 'ambientes' ? 'Ambientes' : 'Procedencias' }}</h1>
    <div class="admin-tabs">
      <button
        class="admin-tabs__button"
        [class.admin-tabs__button--active]="activeTab === 'tabla'"
        (click)="activeTab = 'tabla'"
      >
        Tabla
      </button>
      <button
        class="admin-tabs__button"
        [class.admin-tabs__button--active]="activeTab === 'auditoria'"
        (click)="activeTab = 'auditoria'"
      >
        Auditoría
      </button>
    </div>
    @if (activeTab === 'tabla') {
      <form class="catalog-toolbar" (ngSubmit)="guardar()">
        <input
          class="admin-search"
          [(ngModel)]="busqueda"
          name="busqueda"
          placeholder="Buscar por nombre"
          (ngModelChange)="pagina = 1"
          aria-label="Buscar por nombre"
        />
        <input
          class="admin-search"
          [(ngModel)]="nombre"
          name="nombre"
          placeholder="Nombre"
          maxlength="255"
          aria-label="Nombre del registro"
        />
        <button class="btn btn-primary" [disabled]="!nombre.trim() || ocupado">
          {{ editando ? 'Guardar cambios' : 'Añadir' }}
        </button>
        @if (editando) {
          <button
            type="button"
            class="btn btn-secondary"
            (click)="editando = undefined; nombre = ''"
          >
            Cancelar
          </button>
        }
        <button
          type="button"
          class="btn admin-toolbar-button"
          (click)="exportarCsv(tipo, ['Nombre'], filasExportadas)"
        >
          <i class="fas fa-file-csv"></i> Exportar
        </button>
        <button
          type="button"
          class="btn admin-toolbar-button admin-toolbar-button--icon"
          title="Imprimir tabla"
          aria-label="Imprimir tabla"
          (click)="imprimir()"
        >
          <i class="fas fa-print"></i>
        </button>
      </form>
      @if (mensajeError) {
        <p role="alert">{{ mensajeError }}</p>
      }
      <div class="table-responsive">
        <table class="data-table">
          <thead>
            <tr>
              <th>
                <button
                  class="table-sort-button"
                  (click)="ascendente = !ascendente"
                >
                  Nombre <i class="fas fa-sort"></i>
                </button>
              </th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (
              item of filtrados.slice((pagina - 1) * 10, pagina * 10);
              track item.Id
            ) {
              <tr>
                <td>{{ item.Nombre }}</td>
                <td>
                  <button
                    class="btn-icon"
                    title="Editar"
                    (click)="editando = item.Id; nombre = item.Nombre"
                  >
                    <i class="fas fa-pencil-alt"></i>
                  </button>
                  <button
                    class="btn-icon"
                    title="Eliminar"
                    (click)="eliminando = item.Id"
                  >
                    <i class="fas fa-trash"></i>
                  </button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="2">Sin registros</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <app-table-pagination
        [totalItems]="filtrados.length"
        [page]="pagina"
        [pageSize]="10"
        (pageChange)="pagina = $event"
      />
    } @else {
      <app-audit-panel
        [entidad]="tipo === 'ambientes' ? 'Ambiente' : 'Procedencia'"
        [refreshTrigger]="auditRefresh"
      />
    }
    @if (eliminando) {
      <app-aviso-eliminar
        mensaje="¿Eliminar este registro?"
        (aceptar)="eliminar()"
        (cancelar)="eliminando = undefined"
      />
    }
  `,
  styles: [
    '.catalog-toolbar { display:flex; flex-wrap:wrap; gap:12px; margin:24px 0; align-items:center; } .catalog-toolbar input { flex:1 1 220px; min-width:0; }',
  ],
})
export class CatalogosInventarioComponent extends Tabla implements OnChanges {
  @Input() tipo: TipoCatalogo = 'ambientes';
  items: CatalogoInventario[] = [];
  busqueda = '';
  nombre = '';
  mensajeError = '';
  pagina = 1;
  ascendente = true;
  ocupado = false;
  auditRefresh = 0;
  editando?: number;
  eliminando?: number;
  constructor(private readonly api: CatalogoInventarioService) {
    super();
  }
  ngOnChanges() {
    this.nombre = '';
    this.busqueda = '';
    this.editando = undefined;
    this.pagina = 1;
    this.cargar();
  }
  override aplicarFiltros(): void {
    this.pagina = 1;
  }
  get filasExportadas() {
    return this.filtrados.map((i) => [i.Nombre]);
  }
  imprimir() {
    printTable({
      title: this.tipo === 'ambientes' ? 'Ambientes' : 'Procedencias',
      headers: ['Nombre'],
      rows: this.filasExportadas,
    });
  }
  get filtrados() {
    return this.items
      .filter((i) =>
        i.Nombre.toLocaleLowerCase().includes(
          this.busqueda.toLocaleLowerCase(),
        ),
      )
      .sort(
        (a, b) =>
          a.Nombre.localeCompare(b.Nombre, 'es') * (this.ascendente ? 1 : -1),
      );
  }
  cargar() {
    this.api.listar(this.tipo).subscribe({
      next: (r) => {
        this.items = r;
        this.pagina = Math.min(
          this.pagina,
          Math.max(1, Math.ceil(this.filtrados.length / 10)),
        );
        this.mensajeError = '';
      },
      error: (e) => (this.mensajeError = extractErrorMessage(e)),
    });
  }
  guardar() {
    if (this.ocupado) return;
    this.ocupado = true;
    this.api.guardar(this.tipo, this.nombre, this.editando).subscribe({
      next: () => {
        this.ocupado = false;
        this.nombre = '';
        this.editando = undefined;
        this.auditRefresh++;
        this.cargar();
      },
      error: (e) => {
        this.ocupado = false;
        this.mensajeError = extractErrorMessage(e);
      },
    });
  }
  eliminar() {
    if (!this.eliminando) return;
    this.api.eliminar(this.tipo, this.eliminando).subscribe({
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
