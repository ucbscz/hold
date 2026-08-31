import { CatalogoSelectorComponent } from '../catalogo-selector.component';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Equipos, Gaveteros } from '@entities/admin';
import { EquipoService } from '@entities/equipment';
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import { GaveteroService } from '@entities/locker';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
@Component({
  selector: 'app-equipos-editar',
  imports: [
    ValidatedFormsModule,
    CatalogoSelectorComponent,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
  ],
  templateUrl: './equipos-editar.component.html',
  styleUrl: './equipos-editar.component.css',
})
export class EquiposEditarComponent extends BaseTablaComponent {
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Output() actualizar: EventEmitter<void> = new EventEmitter<void>();
  @Input() equipo: Equipos = new Equipos();
  grupoequipo: GrupoEquipo[] = [];
  grupoequipoSeleccionado: GrupoEquipo | null = null;
  gaveteros: Gaveteros[] = [];
  gaveteroSeleccionado: Gaveteros | null = null;
  estadoOpciones: OpcionSelect[] = [
    { value: 'operativo', label: 'Operativo' },
    { value: 'parcialmente_operativo', label: 'Parcialmente Operativo' },
    { value: 'inoperativo', label: 'Inoperativo' },
  ];
  get gruposOpciones(): OpcionSelect[] {
    const actual = `${this.equipo.NombreGrupoEquipo} ${this.equipo.Modelo} ${this.equipo.Marca} (Actual)`;
    return [
      { value: null, label: actual },
      ...this.grupoequipo.map((g) => ({
        value: g,
        label: `${g.nombre} ${g.modelo} ${g.marca}`,
      })),
    ];
  }
  get gaveterosOpciones(): OpcionSelect[] {
    return [
      { value: null, label: 'Sin gavetero' },
      ...this.gaveteros.map((g) => ({ value: g, label: g.Nombre ?? '' })),
    ];
  }
  constructor(
    private readonly equipoapi: EquipoService,
    private grupoequipoAPI: GrupoequipoService,
    private gaveterosAPI: GaveteroService,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarGruposEquipos();
    this.cargarGaveteros();
  }
  cargarGaveteros() {
    this.gaveterosAPI.obtenerGaveteros().subscribe({
      next: (data) => {
        this.gaveteros = data;
        this.gaveteroSeleccionado =
          this.gaveteros.find((g) => g.Id === this.equipo.IdGavetero) ?? null;
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar gaveteros',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  cargarGruposEquipos() {
    this.grupoequipoAPI.obtenersinfiltroGruposEquipos().subscribe({
      next: (data) => {
        this.grupoequipo = data;
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar grupos de equipos',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  validaredicion() {
    this.mensajeaviso =
      '¿Confirma que desea editar el equipo ' +
      this.equipo.NombreGrupoEquipo +
      ' ?';
    this.aviso.set(true);
  }
  confirmar() {
    if (this.grupoequipoSeleccionado) {
      this.equipo.IdGrupoEquipo = this.grupoequipoSeleccionado.id;
      this.equipo.NombreGrupoEquipo = this.grupoequipoSeleccionado.nombre;
      this.equipo.Marca = this.grupoequipoSeleccionado.marca ?? null;
      this.equipo.Modelo = this.grupoequipoSeleccionado.modelo ?? null;
    }
    this.equipo.IdGavetero = this.gaveteroSeleccionado?.Id ?? null;
    this.equipoapi.editarEquipo(this.equipo).subscribe({
      next: () => {
        this.actualizar.emit();
        this.mensajeexito = 'Equipo editado con exito';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al editar el equipo',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  cerrar() {
    this.botoneditar.set(false);
  }
  @HostListener('click', ['$event'])
  onOverlayClick(event: MouseEvent) {
    if (event.target === event.currentTarget) this.cerrar();
  }
}
