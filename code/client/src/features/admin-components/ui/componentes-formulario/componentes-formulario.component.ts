import { Component, inject, OnInit } from '@angular/core';
import { Componente, ComponenteService } from '@entities/component';
import { Equipos, EquipoService } from '@entities/equipment';
import {
  FormularioAdminComponent,
  FormularioAdminEnvioComponent,
  FormularioAdminMarcoComponent,
} from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';

@Component({
  selector: 'app-componentes-formulario',
  imports: [
    FormularioAdminMarcoComponent,
    FormularioAdminEnvioComponent,
    ValidatedFormsModule,
    CustomSelectComponent,
  ],
  templateUrl: './componentes-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class ComponentesFormularioComponent
  extends FormularioAdminComponent<Componente>
  implements OnInit
{
  private readonly servicio = inject(ComponenteService);
  private readonly equipoService = inject(EquipoService);
  protected readonly entidad = { nombre: 'componente' };
  equipos: Equipos[] = [];

  get equiposOpciones(): OpcionSelect[] {
    return this.equipos.map((e) => ({
      value: e.Id,
      label: `${e.NombreGrupoEquipo} ${e.CodigoImt}`,
    }));
  }

  ngOnInit(): void {
    this.cargarOpciones(
      this.equipoService.obtenerEquipos(),
      (equipos) => (this.equipos = equipos),
      'No se pudieron cargar los equipos.',
    );
  }

  protected crearModelo(): Componente {
    return new Componente();
  }

  protected crear(registro: Componente) {
    return this.servicio.crearComponente(registro);
  }

  protected editar(registro: Componente) {
    return this.servicio.actualizarComponente(registro);
  }
}
