import { CatalogoSelectorComponent } from '../catalogo-selector.component';
import {
  Component,
  EventEmitter,
  inject,
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
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
  ToastService,
} from '@shared/ui';
@Component({
  selector: 'app-equipos-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    CatalogoSelectorComponent,
    MostrarerrorComponent,
    Aviso,
    CustomSelectComponent,
  ],
  templateUrl: './equipos-crear.component.html',
  styleUrl: './equipos-crear.component.css',
})
export class EquiposCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  grupoequipo: GrupoEquipo[] = [];
  equipo: Equipos = new Equipos();
  grupoequipoSeleccionado: GrupoEquipo | null = null;
  gaveteros: Gaveteros[] = [];
  gaveteroSeleccionado: Gaveteros | null = null;
  estadoOpciones: OpcionSelect[] = [
    { value: 'operativo', label: 'Operativo' },
    { value: 'parcialmente_operativo', label: 'Parcialmente Operativo' },
    { value: 'inoperativo', label: 'Inoperativo' },
  ];
  get gruposOpciones(): OpcionSelect[] {
    return this.grupoequipo.map((g) => ({
      value: g,
      label: `${g.nombre} ${g.modelo} ${g.marca}`,
    }));
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
    this.equipo.EstadoEquipo = 'operativo';
    this.cargarGruposEquipos();
    this.cargarGaveteros();
  }
  cargarGaveteros() {
    this.gaveterosAPI.obtenerGaveteros().subscribe({
      next: (data) => {
        this.gaveteros = data;
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los gaveteros. Intente mas tarde',
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
          'Error al cargar los grupos equipos. Intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  validarcreacion() {
    if (!this.grupoequipoSeleccionado) {
      this.mensajeerror = 'Debe seleccionar un grupo de equipo.';
      this.error.set(true);
      return;
    }
    this.mensajeaviso = 'Esta seguro de crear el equipo?';
    this.aviso.set(true);
  }
  registrar() {
    if (!this.iniciarEnvio()) return;
    this.equipo.IdGrupoEquipo = this.grupoequipoSeleccionado!.id;
    this.equipo.NombreGrupoEquipo = this.grupoequipoSeleccionado!.nombre;
    this.equipo.IdGavetero = this.gaveteroSeleccionado?.Id ?? null;
    this.equipo.Marca = this.grupoequipoSeleccionado!.marca ?? null;
    this.equipo.Modelo = this.grupoequipoSeleccionado!.modelo ?? null;
    this.equipoapi.crearEquipo(this.equipo).subscribe({
      next: () => {
        this.Actualizar.emit();
        this.grupoequipoSeleccionado = null;
        this.finalizarEnvio();
        this.toast.success('Equipo creado con éxito.');
        this.cerrar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al crear el equipo. Intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
        this.finalizarEnvio();
      },
    });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
}
