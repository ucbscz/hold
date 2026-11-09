import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PrestamosAPIService } from '@entities/loan';
import { extractErrorMessage } from '@shared/lib/error';
import { finalize } from 'rxjs';

export type DecisionPrestamo = 'aprobado' | 'rechazado';

@Component({
  selector: 'app-decision-prestamo',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './decision-prestamo.component.html',
  styleUrls: [
    '../../../../shared/lib/admin-table/tabla-admin.css',
    '../modal-prestamo.css',
    './decision-prestamo.component.css',
  ],
})
export class DecisionPrestamoComponent {
  private readonly prestamosapi = inject(PrestamosAPIService);
  @Input({ required: true }) decision!: DecisionPrestamo;
  @Input({ required: true }) prestamoId!: number;
  @Output() cerrar = new EventEmitter<void>();
  @Output() decidido = new EventEmitter<string>();
  @Output() fallo = new EventEmitter<string>();
  detalle = '';
  procesando = false;

  confirmar(): void {
    const detalle = this.detalle.trim();
    const aprobar = this.decision === 'aprobado';
    if (this.procesando || (!aprobar && !detalle)) return;

    this.procesando = true;
    this.prestamosapi
      .cambiarEstadoPrestamo(
        this.prestamoId,
        this.decision,
        detalle || undefined,
      )
      .pipe(finalize(() => (this.procesando = false)))
      .subscribe({
        next: () => {
          this.procesando = false;
          this.decidido.emit(
            aprobar
              ? 'Préstamo aprobado con éxito.'
              : 'Préstamo rechazado con éxito.',
          );
        },
        error: (error) =>
          this.fallo.emit(
            `Error al ${aprobar ? 'aprobar' : 'rechazar'} el préstamo: ${extractErrorMessage(error)}`,
          ),
      });
  }

  salir(): void {
    if (!this.procesando) this.cerrar.emit();
  }
}
