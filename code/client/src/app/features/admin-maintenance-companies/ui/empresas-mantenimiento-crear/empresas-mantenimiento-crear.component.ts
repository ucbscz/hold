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
import { EmpresaMantenimiento } from '@entities/admin';
import { EmpresamantenimientoService } from '@entities/maintenance-company';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, MostrarerrorComponent, ToastService } from '@shared/ui';
@Component({
  selector: 'app-empresas-mantenimiento-crear',
  standalone: true,
  imports: [ValidatedFormsModule, MostrarerrorComponent, Aviso],
  templateUrl: './empresas-mantenimiento-crear.component.html',
  styleUrl: './empresas-mantenimiento-crear.component.css',
})
export class EmpresasMantenimientoCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
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
    if (!this.iniciarEnvio()) return;
    this.empresaMantenimientoapi
      .crearEmpresaMantenimiento(this.empresaMantenimiento)
      .subscribe({
        next: () => {
          this.Actualizar.emit();
          this.finalizarEnvio();
          this.toast.success('Empresa de mantenimiento creada correctamente.');
          this.cerrar();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al crear la empresa, Intente mas tarde',
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
