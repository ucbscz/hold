import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Mantenimientos } from '@entities/admin';
import {
  MantenimientosAgrupados,
  MantenimientoService,
} from '@entities/maintenance';
import { BuscadorComponent } from '@features/admin-search';
import { Tabla, TablePaginationComponent } from '@shared/lib/admin-table';
import {
  FlatpickrDirective,
  StickyScrollDirective,
} from '@shared/lib/directives';
import { extractErrorMessage } from '@shared/lib/error';
import {
  AvisoEliminarComponent,
  AvisoExitoComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { AuditPanelComponent } from '@widgets/audit-panel';
import { MantenimientosCrearComponent } from '../mantenimientos-crear/mantenimientos-crear.component';
import { DetallesMantenimientoComponent } from './detalles-mantenimiento/detalles-mantenimiento.component';
@Component({
  selector: 'app-mantenimientos-tabla',
  standalone: true,
  imports: [
    StickyScrollDirective,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MantenimientosCrearComponent,
    DetallesMantenimientoComponent,
    AvisoEliminarComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    BuscadorComponent,
    AuditPanelComponent,
    FlatpickrDirective,
    TablePaginationComponent,
  ],
  templateUrl: './mantenimientos-tabla.component.html',
  styleUrl: './mantenimientos-tabla.component.css',
})
export class MantenimientosTablaComponent extends Tabla implements OnInit {
  expandedRowId: number | null = null;
  auditRefresh = 0;

  toggleExpand(id: number) {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }
  botoncrear: WritableSignal<boolean> = signal(false);
  mostrardetalles: WritableSignal<boolean> = signal(false);
  alertaeliminar: boolean = false;
  mantenimientos: MantenimientosAgrupados[] = [];
  mantenimientoGruposeleccionado: Mantenimientos[] = [];
  mantenimientosFiltrados: MantenimientosAgrupados[] = [];
  mantenimientoSeleccionado: Mantenimientos = new Mantenimientos();
  fechaInicioDesde: Date | null = null;
  fechaFinHasta: Date | null = null;
  filtroBusqueda: [string, string] = ['', ''];
  override columnas: string[] = [
    'Empresa',
    'Códigos IMT',
    'Fecha Inicio',
    'Fecha Fin',
    'Costo',
  ];
  constructor(private readonly mantenimientoapi: MantenimientoService) {
    super();
  }
  ngOnInit() {
    this.cargarMantenimientos();
  }
  limpiarMantenimientoSeleccionado() {
    this.mantenimientoSeleccionado = new Mantenimientos();
  }
  crearmantenimiento() {
    this.botoncrear.set(true);
  }
  cargarMantenimientos() {
    this.mantenimientoapi.obtenerMantenimientos().subscribe({
      next: (datos) => {
        this.agruparMantenimientos(datos);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los mantenimientos, intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  agruparMantenimientos(datos: Mantenimientos[]) {
    this.mantenimientos = [];
    if (datos.length === 0) {
      this.mantenimientosFiltrados = [];
      return;
    }
    let mantenimientosArray: Mantenimientos[] = [];
    for (let i = 0; i < datos.length; i++) {
      mantenimientosArray.push(datos[i]);
      if (i === datos.length - 1 || datos[i].Id !== datos[i + 1]?.Id) {
        this.mantenimientos.push(
          new MantenimientosAgrupados(mantenimientosArray),
        );
        mantenimientosArray = [];
      }
    }
    this.mantenimientosFiltrados = [...this.mantenimientos];
    this.aplicarFiltros();
  }
  buscar() {
    this.aplicarFiltros();
  }
  aplicarFiltros(event?: [string, string]) {
    if (event) this.filtroBusqueda = event;

    const busquedaNormalizada = this.normalizeText(this.filtroBusqueda[0]);
    const columna = this.filtroBusqueda[1];
    const fechaInicio = this.inicioDelDia(this.fechaInicioDesde);
    const fechaFin = this.finDelDia(this.fechaFinHasta);

    this.mantenimientosFiltrados = this.mantenimientos.filter(
      (mantenimiento) => {
        const coincideBusqueda = !busquedaNormalizada
          ? true
          : (() => {
              switch (columna) {
                case 'Empresa':
                  return this.normalizeText(
                    mantenimiento.datosgrupo.NombreEmpresaMantenimiento || '',
                  ).includes(busquedaNormalizada);
                case 'Códigos IMT':
                  return this.normalizeText(
                    String(mantenimiento.datosgrupo.CodigoImtEquipo || ''),
                  ).includes(busquedaNormalizada);
                case 'Fecha Inicio':
                  const fechaFormateada = this.formatDate(
                    mantenimiento.datosgrupo.FechaMantenimiento,
                  );
                  return this.normalizeText(fechaFormateada).includes(
                    busquedaNormalizada,
                  );
                case 'Fecha Fin':
                  return this.normalizeText(
                    this.formatDate(
                      mantenimiento.datosgrupo.FechaFinalDeMantenimiento,
                    ),
                  ).includes(busquedaNormalizada);
                case 'Costo':
                  return this.normalizeText(
                    String(mantenimiento.datosgrupo.Costo || ''),
                  ).includes(busquedaNormalizada);
                default:
                  return (
                    this.normalizeText(
                      mantenimiento.datosgrupo.NombreEmpresaMantenimiento || '',
                    ).includes(busquedaNormalizada) ||
                    this.normalizeText(
                      mantenimiento.datosgrupo.TipoMantenimiento || '',
                    ).includes(busquedaNormalizada) ||
                    this.normalizeText(
                      mantenimiento.datosgrupo.NombreGrupoEquipo || '',
                    ).includes(busquedaNormalizada) ||
                    this.normalizeText(
                      String(mantenimiento.datosgrupo.CodigoImtEquipo || ''),
                    ).includes(busquedaNormalizada) ||
                    this.normalizeText(
                      this.formatDate(
                        mantenimiento.datosgrupo.FechaMantenimiento,
                      ),
                    ).includes(busquedaNormalizada) ||
                    this.normalizeText(
                      this.formatDate(
                        mantenimiento.datosgrupo.FechaFinalDeMantenimiento,
                      ),
                    ).includes(busquedaNormalizada) ||
                    this.normalizeText(
                      String(mantenimiento.datosgrupo.Costo || ''),
                    ).includes(busquedaNormalizada)
                  );
              }
            })();
        const inicio = this.fechaValida(
          mantenimiento.datosgrupo.FechaMantenimiento,
        );
        const fin = this.fechaValida(
          mantenimiento.datosgrupo.FechaFinalDeMantenimiento,
        );
        const coincideInicio =
          !fechaInicio || (!!inicio && inicio >= fechaInicio);
        const coincideFin = !fechaFin || (!!fin && fin <= fechaFin);

        return coincideBusqueda && coincideInicio && coincideFin;
      },
    );
    this.reiniciarPaginacion();
    this.aplicarOrdenActualSiExiste();
  }

  onFechaInicioDesde(dates: Date[]): void {
    this.fechaInicioDesde = dates[0] ?? null;
    this.aplicarFiltros();
  }

  onFechaFinHasta(dates: Date[]): void {
    this.fechaFinHasta = dates[0] ?? null;
    this.aplicarFiltros();
  }

  private fechaValida(valor: Date | string | null): Date | null {
    if (!valor) return null;
    const fecha = new Date(valor);
    return Number.isNaN(fecha.getTime()) ? null : fecha;
  }

  private inicioDelDia(valor: Date | null): Date | null {
    if (!valor) return null;
    const fecha = new Date(valor);
    fecha.setHours(0, 0, 0, 0);
    return fecha;
  }

  private finDelDia(valor: Date | null): Date | null {
    if (!valor) return null;
    const fecha = new Date(valor);
    fecha.setHours(23, 59, 59, 999);
    return fecha;
  }
  limpiarBusqueda() {
    this.filtroBusqueda = ['', ''];
    this.aplicarFiltros();
  }
  eliminarMantenimiento(mantenimiento: MantenimientosAgrupados) {
    this.mantenimientoSeleccionado = mantenimiento.matenimientos[0];
    this.alertaeliminar = true;
  }
  confirmarEliminacion() {
    this.mantenimientoapi
      .eliminarMantenimiento(this.mantenimientoSeleccionado.Id)
      .subscribe({
        next: () => {
          this.limpiarMantenimientoSeleccionado();
          this.alertaeliminar = false;
          this.mensajeexito = 'Mantenimiento eliminado exitosamente';
          this.exito.set(true);
          this.auditRefresh++;
          this.cargarMantenimientos();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al eliminar el mantenimiento, intente mas tarde',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
          this.limpiarMantenimientoSeleccionado();
          this.alertaeliminar = false;
        },
      });
  }
  cancelarEliminacion() {
    this.alertaeliminar = false;
    this.limpiarMantenimientoSeleccionado();
  }
  mostrarmantenimientosindividuales(
    mantenimientosgrupo: MantenimientosAgrupados,
  ) {
    this.mantenimientoGruposeleccionado = mantenimientosgrupo.matenimientos;
    this.mostrardetalles.set(true);
  }

  override sortTable(e: { col: string; dir: 'asc' | 'desc' }): void {
    this.mantenimientosFiltrados = this.sortByColumn(
      this.mantenimientosFiltrados,
      e,
      {
        Empresa: (mantenimiento) =>
          mantenimiento.datosgrupo.NombreEmpresaMantenimiento,
        'Códigos IMT': (mantenimiento) =>
          mantenimiento.datosgrupo.CodigoImtEquipo,
        'Fecha Inicio': (mantenimiento) =>
          mantenimiento.datosgrupo.FechaMantenimiento,
        'Fecha Fin': (mantenimiento) =>
          mantenimiento.datosgrupo.FechaFinalDeMantenimiento,
        Costo: (mantenimiento) => mantenimiento.datosgrupo.Costo,
        'Tipo y nombre equipos': (mantenimiento) =>
          `${mantenimiento.datosgrupo.TipoMantenimiento ?? ''} ${mantenimiento.datosgrupo.NombreGrupoEquipo ?? ''}`,
      },
    );
  }
}
