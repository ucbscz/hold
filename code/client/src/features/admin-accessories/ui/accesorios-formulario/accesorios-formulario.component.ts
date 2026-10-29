import { Component, inject, OnInit } from '@angular/core';
import { Accesorio, AccesoriosService } from '@entities/accessory';
import { Equipos, EquipoService } from '@entities/equipment';
import {
  FormularioAdminComponent,
  FormularioAdminEnvioComponent,
  FormularioAdminMarcoComponent,
} from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';

@Component({
  selector: 'app-accesorios-formulario',
  imports: [
    FormularioAdminMarcoComponent,
    FormularioAdminEnvioComponent,
    ValidatedFormsModule,
    CustomSelectComponent,
  ],
  templateUrl: './accesorios-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class AccesoriosFormularioComponent
  extends FormularioAdminComponent<Accesorio>
  implements OnInit
{
  private readonly servicio = inject(AccesoriosService);
  private readonly equipoService = inject(EquipoService);
  protected readonly entidad = { nombre: 'accesorio' };
  equipos: Equipos[] = [];

  get equiposOpciones(): OpcionSelect[] {
    return this.equipos.map((e) => ({
      value: e.Id,
      label: `${e.NombreGrupoEquipo} - ${e.CodigoImt}`,
    }));
  }

  ngOnInit(): void {
    this.cargarOpciones(
      this.equipoService.obtenerEquipos(),
      (equipos) => (this.equipos = equipos),
      'No se pudieron cargar los equipos.',
    );
  }

  protected crearModelo(): Accesorio {
    return new Accesorio();
  }

  protected crear(registro: Accesorio) {
    return this.servicio.crearAccesorio(registro);
  }

  protected editar(registro: Accesorio) {
    return this.servicio.editarAccesorio(registro);
  }
}
