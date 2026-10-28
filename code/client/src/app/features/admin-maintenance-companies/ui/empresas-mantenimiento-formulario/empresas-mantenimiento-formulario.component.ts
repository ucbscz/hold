import { Component, inject } from '@angular/core';
import { EmpresaMantenimiento } from '@entities/admin';
import { EmpresamantenimientoService } from '@entities/maintenance-company';
import { FormularioAdminComponent } from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Aviso, MostrarerrorComponent } from '@shared/ui';

@Component({
  selector: 'app-empresas-mantenimiento-formulario',
  imports: [ValidatedFormsModule, MostrarerrorComponent, Aviso],
  templateUrl: './empresas-mantenimiento-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class EmpresasMantenimientoFormularioComponent extends FormularioAdminComponent<EmpresaMantenimiento> {
  private readonly servicio = inject(EmpresamantenimientoService);
  protected readonly entidad = {
    nombre: 'empresa de mantenimiento',
    femenino: true,
  };

  protected crearModelo(): EmpresaMantenimiento {
    return new EmpresaMantenimiento();
  }

  protected crear(registro: EmpresaMantenimiento) {
    return this.servicio.crearEmpresaMantenimiento(registro);
  }

  protected editar(registro: EmpresaMantenimiento) {
    return this.servicio.actualizarEmpresaMantenimiento(registro);
  }
}
