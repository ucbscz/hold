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
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  CustomSelectComponent,
  MostrarerrorComponent,
  ToastService,
} from '@shared/ui';
@Component({
  selector: 'app-grupos-equipos-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    CustomSelectComponent,
  ],
  templateUrl: './grupos-equipos-crear.component.html',
  styleUrl: './grupos-equipos-crear.component.css',
})
export class GruposEquiposCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Input() categorias: string[] = [];
  @Output() Actualizar = new EventEmitter<void>();
  grupoEquipo: GrupoEquipo = new GrupoEquipo();
  constructor(private readonly grupoEquipoapi: GrupoequipoService) {
    super();
  }
  validarregistro() {
    this.mensajeaviso = 'Desea registrar el nuevo grupo de equipo?';
    this.aviso.set(true);
  }
  registrar() {
    if (!this.iniciarEnvio()) return;
    this.grupoEquipoapi.crearGrupoEquipo(this.grupoEquipo).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.finalizarEnvio();
        this.toast.success('Grupo de equipo registrado exitosamente.');
        this.cerrar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al registrar el grupo de equipo',
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
