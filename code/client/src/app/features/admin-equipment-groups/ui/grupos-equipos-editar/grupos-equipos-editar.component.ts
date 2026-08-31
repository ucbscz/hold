import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
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
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
} from '@shared/ui';
@Component({
  selector: 'app-grupos-equipos-editar',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
  ],
  templateUrl: './grupos-equipos-editar.component.html',
  styleUrl: './grupos-equipos-editar.component.css',
})
export class GruposEquiposEditarComponent
  extends BaseTablaComponent
  implements OnChanges
{
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Output() actualizar: EventEmitter<void> = new EventEmitter<void>();
  @Input() categorias: string[] = [];
  @Input() grupoequipo: GrupoEquipo = new GrupoEquipo();
  grupoEquipo: GrupoEquipo = { ...this.grupoequipo };
  constructor(private readonly grupoEquipoapi: GrupoequipoService) {
    super();
  }
  ngOnChanges() {
    this.grupoEquipo = { ...this.grupoequipo };
  }
  validaredicion() {
    this.mensajeaviso =
      '¿Desea guardar los cambios realizados al grupo de equipo?';
    this.aviso.set(true);
  }
  confirmar() {
    this.grupoEquipoapi.editarGrupoEquipo(this.grupoEquipo).subscribe({
      next: (_response) => {
        this.actualizar.emit();
        this.mensajeexito = 'Grupo de equipo editado exitosamente';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al editar el grupo de equipo',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  cerrar() {
    this.botoneditar.set(false);
  }
  @HostListener('click', ['$event'])
  onOverlayClick(event: MouseEvent) {
    if (event.target === event.currentTarget) this.cerrar();
  }
}
