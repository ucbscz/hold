import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Carrera } from '@entities/admin';
import { CarreraService } from '@entities/career';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, AvisoExitoComponent, MostrarerrorComponent } from '@shared/ui';
@Component({
  selector: 'app-carreras-editar',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
  ],
  templateUrl: './carreras-editar.component.html',
  styleUrl: './carreras-editar.component.css',
})
export class CarrerasEditarComponent extends BaseTablaComponent {
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Output() actualizar: EventEmitter<void> = new EventEmitter<void>();
  @Input() carrera: Carrera = new Carrera();
  constructor(private readonly carreraService: CarreraService) {
    super();
  }
  validaredicion() {
    if (!this.carrera.Nombre || this.carrera.Nombre.trim() === '') {
      this.mensajeerror = 'El nombre de la carrera no puede estar vacío.';
      this.error.set(true);
      return;
    }
    this.mensajeaviso = '¿Está seguro de que desea  editar esta carrera?';
    this.aviso.set(true);
  }
  confirmar() {
    this.carreraService.actualizarCarrera(this.carrera).subscribe({
      next: (_response) => {
        this.actualizar.emit();
        this.mensajeexito = 'Carrera editada con éxito.';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'error al actualizar la carrera',
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
