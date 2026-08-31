import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { EmpresaMantenimiento } from '@entities/admin';
import { EmpresamantenimientoService } from '@entities/maintenance-company';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, AvisoExitoComponent, MostrarerrorComponent } from '@shared/ui';
@Component({
  selector: 'app-empresas-mantenimiento-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
  ],
  templateUrl: './empresas-mantenimiento-crear.component.html',
  styleUrl: './empresas-mantenimiento-crear.component.css',
})
export class EmpresasMantenimientoCrearComponent extends BaseTablaComponent {
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  empresaMantenimiento: EmpresaMantenimiento = new EmpresaMantenimiento();
  constructor(
    private readonly empresaMantenimientoapi: EmpresamantenimientoService,
  ) {
    super();
  }
  validarregistro() {
    this.mensajeaviso = 'Esta seguro de crear este empresa?';
    this.aviso.set(true);
  }
  registrar() {
    this.empresaMantenimientoapi
      .crearEmpresaMantenimiento(this.empresaMantenimiento)
      .subscribe({
        next: () => {
          this.Actualizar.emit();
          this.mensajeexito =
            'Empresa de mantenimiento creada Satisfactoriamente';
          this.exito.set(true);
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al crear la empresa, Intente mas tarde',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
}
