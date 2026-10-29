import { Component, inject, OnInit } from '@angular/core';
import { Gaveteros, GaveteroService } from '@entities/locker';
import { MuebleService } from '@entities/furniture';
import {
  FormularioAdminComponent,
  FormularioAdminEnvioComponent,
  FormularioAdminMarcoComponent,
} from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { CustomSelectComponent } from '@shared/ui';

@Component({
  selector: 'app-gaveteros-formulario',
  imports: [
    FormularioAdminMarcoComponent,
    FormularioAdminEnvioComponent,
    ValidatedFormsModule,
    CustomSelectComponent,
  ],
  templateUrl: './gaveteros-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class GaveterosFormularioComponent
  extends FormularioAdminComponent<Gaveteros>
  implements OnInit
{
  private readonly servicio = inject(GaveteroService);
  private readonly muebleService = inject(MuebleService);
  protected readonly entidad = { nombre: 'gavetero' };
  muebles: string[] = [];

  ngOnInit(): void {
    this.cargarOpciones(
      this.muebleService.obtenerMuebles(),
      (muebles) => (this.muebles = muebles.map((m) => m.Nombre ?? '')),
      'No se pudieron cargar los muebles.',
    );
  }

  protected crearModelo(): Gaveteros {
    return new Gaveteros();
  }

  protected crear(registro: Gaveteros) {
    return this.servicio.crearGavetero(registro);
  }

  protected editar(registro: Gaveteros) {
    return this.servicio.editarGavetero(registro);
  }
}
