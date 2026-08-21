import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EmpresaMantenimiento } from '@entities/admin';
import { EmpresamantenimientoService } from '@entities/maintenance-company';
import { BuscadorComponent } from '@features/admin-search';
import { Tabla } from '@shared/lib/admin-table';
import { StickyScrollDirective } from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoEliminarComponent,
  AvisoExitoComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { EmpresasMantenimientoCrearComponent } from '../empresas-mantenimiento-crear/empresas-mantenimiento-crear.component';
import { EmpresasMantenimientoEditarComponent } from '../empresas-mantenimiento-editar/empresas-mantenimiento-editar.component';
@Component({
  selector: 'app-empresas-mantenimiento-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    EmpresasMantenimientoCrearComponent,
    EmpresasMantenimientoEditarComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    AuditPanelComponent,
  ],
  templateUrl: './empresas-mantenimiento-tabla.component.html',
  styleUrl: './empresas-mantenimiento-tabla.component.css',
})
export class EmpresasMantenimientoTablaComponent
  extends Tabla
  implements OnInit
{
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }
  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  empresas: EmpresaMantenimiento[] = [];
  empresascopia: EmpresaMantenimiento[] = [];
  empresaSeleccionada: EmpresaMantenimiento = new EmpresaMantenimiento();
  override columnas: string[] = [
    'Nombre Empresa',
    'Responsable',
    'Teléfono',
    'NIT',
  ];
  constructor(private readonly empresaService: EmpresamantenimientoService) {
    super();
  }
  ngOnInit() {
    this.cargarEmpresas();
  }
  limpiarEmpresaSeleccionada() {
    this.empresaSeleccionada = new EmpresaMantenimiento();
  }
  crearempresamantenimiento() {
    this.botoneditar.set(false);
    this.botoncrear.set(true);
  }
  cargarEmpresas() {
    this.empresaService.obtenerEmpresaMantenimiento().subscribe({
      next: (data: EmpresaMantenimiento[]) => {
        this.empresas = data;
        this.empresascopia = [...this.empresas];
        this.aplicarFiltros();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar las empresas de mantenimiento, intente más tarde.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  aplicarFiltros(event?: [string, string]) {
    const busqueda = this.mantenerBusqueda(event);
    if (busqueda[0].trim() !== '') {
      const busquedaNormalizada = this.normalizeText(busqueda[0]);
      this.empresas = this.empresascopia.filter((empresa) => {
        switch (busqueda[1]) {
          case 'Nombre Empresa':
            return this.normalizeText(empresa.NombreEmpresa || '').includes(
              busquedaNormalizada,
            );
          case 'Responsable':
            return this.normalizeText(
              empresa.NombreResponsable ||
                '' + empresa.ApellidoResponsable ||
                '',
            ).includes(busquedaNormalizada);
          case 'Teléfono':
            return this.normalizeText(empresa.Telefono || '').includes(
              busquedaNormalizada,
            );
          case 'NIT':
            return this.normalizeText(empresa.Nit || '').includes(
              busquedaNormalizada,
            );
          default:
            return (
              this.normalizeText(empresa.NombreEmpresa || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(empresa.NombreResponsable || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(empresa.Telefono || '').includes(
                busquedaNormalizada,
              ) ||
              this.normalizeText(empresa.Nit || '').includes(
                busquedaNormalizada,
              )
            );
        }
      });
    } else {
      this.empresas = [...this.empresascopia];
    }
    this.aplicarOrdenActualSiExiste();
  }
  limpiarBusqueda() {
    this.limpiarBusquedaPersistida();
    this.empresas = [...this.empresascopia];
    this.aplicarOrdenActualSiExiste();
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.empresas = this.sortByColumn(this.empresas, e, {
      'Nombre Empresa': (empresa) => empresa.NombreEmpresa,
      Responsable: (empresa) =>
        `${empresa.NombreResponsable ?? ''} ${empresa.ApellidoResponsable ?? ''}`,
      Teléfono: (empresa) => empresa.Telefono,
      NIT: (empresa) => empresa.Nit,
    });
  }

  editarEmpresaMantenimiento(empresa: EmpresaMantenimiento) {
    this.botoncrear.set(false);
    this.empresaSeleccionada = { ...empresa };
    this.botoneditar.set(true);
  }
  eliminarEmpresaMantenimiento(empresa: EmpresaMantenimiento) {
    this.empresaSeleccionada = empresa;
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    if (this.empresaSeleccionada.Id) {
      this.empresaService
        .eliminarEmpresaMantenimiento(this.empresaSeleccionada.Id)
        .subscribe({
          next: (_response) => {
            this.cargarEmpresas();
            this.mensajeexito =
              'Empresa de mantenimiento eliminada exitosamente.';
            this.exito.set(true);
            this.auditRefresh++;
          },
          error: (error) => {
            const errorMsg = extractErrorMessage(
              error,
              'Error al eliminar la empresa de mantenimiento.',
            );
            this.mensajeerror = errorMsg;
            this.error.set(true);
          },
        });
    }
    this.limpiarEmpresaSeleccionada();
    this.alertaeliminar = false;
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.limpiarEmpresaSeleccionada();
  }
}
