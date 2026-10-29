import { Component, inject } from '@angular/core';
import { Carrera } from '@entities/admin';
import { CarreraService } from '@entities/career';
import {
  FormularioAdminComponent,
  FormularioAdminEnvioComponent,
  FormularioAdminMarcoComponent,
} from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';

@Component({
  selector: 'app-carreras-formulario',
  imports: [
    FormularioAdminMarcoComponent,
    FormularioAdminEnvioComponent,
    ValidatedFormsModule,
  ],
  templateUrl: './carreras-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class CarrerasFormularioComponent extends FormularioAdminComponent<Carrera> {
  private readonly servicio = inject(CarreraService);
  protected readonly entidad = { nombre: 'carrera', femenino: true };

  protected crearModelo(): Carrera {
    return new Carrera();
  }

  protected override validar(): string | null {
    return this.modelo.Nombre?.trim()
      ? null
      : 'El nombre de la carrera no puede estar vacío.';
  }

  protected crear(registro: Carrera) {
    return this.servicio.crearCarrera(registro.Nombre ?? '');
  }

  protected editar(registro: Carrera) {
    return this.servicio.actualizarCarrera(registro);
  }
}
