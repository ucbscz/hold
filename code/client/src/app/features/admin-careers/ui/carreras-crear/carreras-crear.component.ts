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
import { CarreraService } from '@entities/career';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, MostrarerrorComponent, ToastService } from '@shared/ui';
@Component({
  selector: 'app-carreras-crear',
  standalone: true,
  imports: [ValidatedFormsModule, MostrarerrorComponent, Aviso],
  templateUrl: './carreras-crear.component.html',
  styleUrl: './carreras-crear.component.css',
})
export class CarrerasCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  nombreCarrera: string = '';

  constructor(private readonly carreraService: CarreraService) {
    super();
  }

  validarregistro() {
    if (this.nombreCarrera.trim() === '') {
      this.mensajeerror = 'El nombre de la carrera no puede estar vacío.';
      this.error.set(true);
      return;
    }
    this.mensajeaviso =
      '¿Está seguro de que desea crear la carrera ' + this.nombreCarrera + '?';
    this.aviso.set(true);
  }

  registrar() {
    if (!this.iniciarEnvio()) return;
    this.carreraService.crearCarrera(this.nombreCarrera).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.finalizarEnvio();
        this.toast.success('Carrera creada exitosamente.');
        this.cerrar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al crear la carrera.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
        this.finalizarEnvio();
      },
    });
  }

  cerrar() {
    this.nombreCarrera = '';
    this.botoncrear.set(false);
  }
}
