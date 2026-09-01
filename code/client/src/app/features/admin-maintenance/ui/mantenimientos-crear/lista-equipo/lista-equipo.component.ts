import { CommonModule } from '@angular/common';
import { Component, Input, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Equipos } from '@entities/admin';
import { EquipoService } from '@entities/equipment';
import { AdminTableSort, Tabla } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
import { MantenimientosServiceEquipos } from '../../../model/mantenimientos-equipos.service';

@Component({
  selector: 'app-lista-equipo',
  imports: [
    CommonModule,
    FormsModule,
    CustomSelectComponent,
    MostrarerrorComponent,
  ],
  templateUrl: './lista-equipo.component.html',
  styleUrl: './lista-equipo.component.css',
})
export class ListaEquipoComponent extends Tabla {
  @Input() agregarequipo: WritableSignal<boolean> = signal(true);
  equipos: Equipos[] = [];
  equiposcopia: Equipos[] = [];
  terminoBusqueda: string = '';
  equiposMarcados = new Set<number>();
  grupoSeleccionado: number | null = null;
  gruposOpciones: OpcionSelect[] = [];
  override sortColumn = 'Nombre';
  override columnas = ['Nombre', 'Estado', 'Ubicación', 'Código IMT', 'Costo'];

  get equiposDisponibles(): Equipos[] {
    return this.equipos.filter((equipo) => !this.equipoYaSeleccionado(equipo));
  }

  constructor(
    private readonly equiposapi: EquipoService,
    public mantenimientoequipos: MantenimientosServiceEquipos,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarEquipos();
  }
  alternarEquipo(equipo: Equipos, marcado: boolean): void {
    if (equipo.CodigoImt === null) return;

    if (marcado) this.equiposMarcados.add(equipo.CodigoImt);
    else this.equiposMarcados.delete(equipo.CodigoImt);
  }

  equipoYaSeleccionado(equipo: Equipos): boolean {
    return (
      equipo.CodigoImt !== null &&
      this.mantenimientoequipos.existeEquipo(equipo.CodigoImt)
    );
  }

  equipoMarcado(equipo: Equipos): boolean {
    return equipo.CodigoImt !== null && this.equiposMarcados.has(equipo.CodigoImt);
  }

  alternarTodosVisibles(marcado: boolean): void {
    for (const equipo of this.equiposDisponibles) {
      if (equipo.CodigoImt === null) continue;
      if (marcado) this.equiposMarcados.add(equipo.CodigoImt);
      else this.equiposMarcados.delete(equipo.CodigoImt);
    }
  }

  seleccionarGrupo(): void {
    if (this.grupoSeleccionado === null) return;

    for (const equipo of this.equiposDisponibles) {
      if (
        equipo.IdGrupoEquipo === Number(this.grupoSeleccionado) &&
        equipo.CodigoImt !== null
      ) {
        this.equiposMarcados.add(equipo.CodigoImt);
      }
    }
  }

  agregarSeleccionados(): void {
    for (const equipo of this.equiposDisponibles) {
      if (
        equipo.CodigoImt === null ||
        !this.equiposMarcados.has(equipo.CodigoImt)
      ) {
        continue;
      }

      this.mantenimientoequipos.agregarEquipoMantenimiento(
        equipo.CodigoImt,
        'preventivo',
        '',
        equipo.NombreGrupoEquipo || 'Equipo',
      );
    }

    this.equiposMarcados.clear();
    this.regresar();
  }
  cargarEquipos() {
    this.equiposapi.obtenerEquipos().subscribe({
      next: (data: Equipos[]) => {
        this.equipos = data;
        this.equiposcopia = [...this.equipos];
        this.gruposOpciones = Array.from(
          new Map(
            data
              .filter((equipo) => equipo.IdGrupoEquipo !== null)
              .map((equipo) => [
                equipo.IdGrupoEquipo!,
                equipo.NombreGrupoEquipo || `Grupo ${equipo.IdGrupoEquipo}`,
              ]),
          ),
        )
          .sort((a, b) => a[1].localeCompare(b[1], 'es'))
          .map(([value, label]) => ({ value, label }));
        this.buscar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los equipos, intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  override aplicarFiltros(event?: [string, string]): void {
    this.terminoBusqueda = event?.[0] ?? this.terminoBusqueda;
    this.buscar();
  }
  buscar() {
    if (this.terminoBusqueda.trim() === '') {
      this.limpiarBusqueda();
      return;
    }
    const busquedaNormalizada = this.normalizeText(this.terminoBusqueda);
    this.equipos = this.equiposcopia.filter(
      (equipo) =>
        this.normalizeText(equipo.NombreGrupoEquipo).includes(
          busquedaNormalizada,
        ) ||
        this.normalizeText(equipo.Modelo).includes(busquedaNormalizada) ||
        this.normalizeText(equipo.Marca).includes(busquedaNormalizada) ||
        this.normalizeText(String(equipo.CodigoImt || '')).includes(
          busquedaNormalizada,
        ) ||
        this.normalizeText(equipo.CodigoUcb).includes(busquedaNormalizada) ||
        this.normalizeText(equipo.NumeroSerial).includes(busquedaNormalizada),
    );
    this.aplicarOrdenActualSiExiste();
  }
  limpiarBusqueda() {
    this.terminoBusqueda = '';
    this.equipos = [...this.equiposcopia];
    this.aplicarOrdenActualSiExiste();
  }
  override sortTable(sort: AdminTableSort): void {
    this.equipos = this.sortByColumn(this.equipos, sort, {
      Nombre: (equipo) => equipo.NombreGrupoEquipo,
      Estado: (equipo) => equipo.EstadoEquipo,
      Ubicación: (equipo) => equipo.Ubicacion,
      'Código IMT': (equipo) => equipo.CodigoImt,
      Costo: (equipo) => equipo.CostoReferencia,
    });
  }
  regresar() {
    this.agregarequipo.set(false);
  }
}
