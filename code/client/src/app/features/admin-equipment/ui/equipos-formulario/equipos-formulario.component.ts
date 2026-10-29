import { INVENTORY_LABELS } from '@brand/brand';
import { Component, inject, OnInit } from '@angular/core';
import { Equipos, Gaveteros } from '@entities/admin';
import { ESTADO_EQUIPO_OPCIONES, EquipoService } from '@entities/equipment';
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import { GaveteroService } from '@entities/locker';
import {
  FormularioAdminComponent,
  FormularioAdminEnvioComponent,
  FormularioAdminMarcoComponent,
} from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import { CatalogoSelectorComponent } from '../catalogo-selector.component';

@Component({
  selector: 'app-equipos-formulario',
  imports: [
    FormularioAdminMarcoComponent,
    FormularioAdminEnvioComponent,
    ValidatedFormsModule,
    CatalogoSelectorComponent,
    CustomSelectComponent,
  ],
  templateUrl: './equipos-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class EquiposFormularioComponent
  extends FormularioAdminComponent<Equipos>
  implements OnInit
{
  protected readonly etiquetas = INVENTORY_LABELS;
  private readonly equipoService = inject(EquipoService);
  private readonly grupoEquipoService = inject(GrupoequipoService);
  private readonly gaveteroService = inject(GaveteroService);
  protected readonly entidad = { nombre: 'equipo' };
  protected readonly estadoOpciones = ESTADO_EQUIPO_OPCIONES;
  grupoequipo: GrupoEquipo[] = [];
  grupoequipoSeleccionado: GrupoEquipo | null = null;
  gaveteros: Gaveteros[] = [];
  gaveteroSeleccionado: Gaveteros | null = null;

  get gruposOpciones(): OpcionSelect[] {
    const opciones = this.grupoequipo.map((g) => ({
      value: g,
      label: `${g.nombre} ${g.modelo} ${g.marca}`,
    }));
    if (!this.editando) return opciones;
    const actual = `${this.modelo.NombreGrupoEquipo} (Actual)`;
    return [{ value: null, label: actual }, ...opciones];
  }

  get gaveterosOpciones(): OpcionSelect[] {
    return [
      { value: null, label: 'Sin gavetero' },
      ...this.gaveteros.map((g) => ({ value: g, label: g.Nombre ?? '' })),
    ];
  }

  ngOnInit(): void {
    this.cargarOpciones(
      this.grupoEquipoService.obtenersinfiltroGruposEquipos(),
      (grupos) => (this.grupoequipo = grupos),
      'No se pudieron cargar los grupos de equipos.',
    );
    this.cargarOpciones(
      this.gaveteroService.obtenerGaveteros(),
      (gaveteros) => {
        this.gaveteros = gaveteros;
        this.gaveteroSeleccionado =
          gaveteros.find((g) => g.Id === this.modelo.IdGavetero) ?? null;
      },
      'No se pudieron cargar los gaveteros.',
    );
  }

  protected crearModelo(): Equipos {
    const equipo = new Equipos();
    equipo.EstadoEquipo = 'operativo';
    return equipo;
  }

  protected override validar(): string | null {
    return this.editando || this.grupoequipoSeleccionado
      ? null
      : 'Debe seleccionar un grupo de equipo.';
  }

  protected crear(equipo: Equipos) {
    return this.equipoService.crearEquipo(this.conSeleccion(equipo));
  }

  protected editar(equipo: Equipos) {
    return this.equipoService.editarEquipo(this.conSeleccion(equipo));
  }

  private conSeleccion(equipo: Equipos): Equipos {
    const grupo = this.grupoequipoSeleccionado;
    return {
      ...equipo,
      ...(grupo && {
        IdGrupoEquipo: grupo.id,
        NombreGrupoEquipo: grupo.nombre,
        Marca: grupo.marca ?? null,
        Modelo: grupo.modelo ?? null,
      }),
      IdGavetero: this.gaveteroSeleccionado?.Id ?? null,
    };
  }
}
