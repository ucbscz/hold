import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Mantenimientos } from '@entities/admin';
import { MantenimientoService } from '@entities/maintenance';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
@Component({
  selector: 'app-detalles-mantenimiento',
  imports: [CommonModule, FormsModule, CustomSelectComponent],
  templateUrl: './detalles-mantenimiento.component.html',
  styleUrl: './detalles-mantenimiento.component.css',
})
export class DetallesMantenimientoComponent {
  @Input() mantenimientos: Mantenimientos[] = [];
  @Input() mostrardetalles: WritableSignal<boolean> = signal(true);
  @Output() actualizado = new EventEmitter<void>();
  guardando = false;
  readonly tipoOpciones: OpcionSelect[] = [
    { value: 'preventivo', label: 'Preventivo' },
    { value: 'correctivo', label: 'Correctivo' },
  ];

  constructor(private readonly mantenimientoApi: MantenimientoService) {}

  guardarCambios(): void {
    if (!this.mantenimientos.length || this.guardando) return;

    this.guardando = true;
    this.mantenimientoApi
      .actualizarMantenimiento(this.mantenimientos)
      .subscribe({
        next: () => {
          this.guardando = false;
          this.actualizado.emit();
          this.cerrarDetalles();
        },
        error: () => (this.guardando = false),
      });
  }
  cerrarDetalles() {
    this.mostrardetalles.set(false);
  }
}
