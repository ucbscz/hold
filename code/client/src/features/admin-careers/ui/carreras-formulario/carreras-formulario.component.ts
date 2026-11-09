import { Component, inject } from '@angular/core';
import { Carrera, CarreraService } from '@entities/career';
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
    if (!this.modelo.Nombre?.trim())
      return 'El nombre de la carrera no puede estar vacío.';
    return /^[A-Z0-9]{2,10}$/.test(this.modelo.Codigo ?? '')
      ? null
      : 'El código debe tener de 2 a 10 letras mayúsculas o números.';
  }

  normalizarCodigo(codigo: string): void {
    this.modelo.Codigo = codigo.toUpperCase().replace(/[^A-Z0-9]/g, '');
    this.error.set(false);
  }

  protected crear(registro: Carrera) {
    return this.servicio.crearCarrera(registro);
  }

  protected editar(registro: Carrera) {
    return this.servicio.actualizarCarrera(registro);
  }
}
