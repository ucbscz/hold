import { Component, inject, OnInit } from '@angular/core';
import { Muebles, MuebleService } from '@entities/furniture';
import { CatalogoInventarioService } from '@entities/equipment';
import {
  FormularioAdminComponent,
  FormularioAdminEnvioComponent,
  FormularioAdminMarcoComponent,
} from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';

@Component({
  selector: 'app-muebles-formulario',
  imports: [
    FormularioAdminMarcoComponent,
    FormularioAdminEnvioComponent,
    ValidatedFormsModule,
    CustomSelectComponent,
  ],
  templateUrl: './muebles-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class MueblesFormularioComponent
  extends FormularioAdminComponent<Muebles>
  implements OnInit
{
  private readonly servicio = inject(MuebleService);
  private readonly catalogos = inject(CatalogoInventarioService);
  protected readonly entidad = { nombre: 'mueble' };
  ambientes: OpcionSelect[] = [];

  ngOnInit(): void {
    this.cargarOpciones(
      this.catalogos.listar('ambientes'),
      (items) =>
        (this.ambientes = items.map((a) => ({ value: a.Id, label: a.Nombre }))),
      'No se pudieron cargar los ambientes.',
    );
  }

  protected crearModelo(): Muebles {
    return new Muebles();
  }

  protected crear(registro: Muebles) {
    return this.servicio.crearMueble(registro);
  }

  protected editar(registro: Muebles) {
    return this.servicio.actualizarMueble(registro);
  }
}
