import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { VercontratoComponent } from '../contrato/vercontrato.component';
import { PrestamoDto } from '@entities/admin';
@Component({
  selector: 'app-vista-prestamos',
  imports: [VercontratoComponent],
  templateUrl: './vista-prestamos.component.html',
  styleUrl: './vista-prestamos.component.css',
})
export class VistaPrestamosComponent {
  readonly contratoVisible = signal(false);
  @Input() prestamos: PrestamoDto[] = [];
  @Output() cerrar: EventEmitter<void> = new EventEmitter<void>();
  cerrarVista() {
    this.cerrar.emit();
  }

  observacionTexto(): string {
    const obs = this.prestamos[0]?.Observacion?.trim();
    return obs && obs.toLowerCase() !== 'string' ? obs : 'Sin observación';
  }

  get prestamoPrincipal(): PrestamoDto | null {
    return this.prestamos[0] ?? null;
  }

  get nombreSolicitante(): string {
    const prestamo = this.prestamoPrincipal;
    if (!prestamo) return 'Sin solicitante registrado';

    return (
      [prestamo.NombreUsuario, prestamo.ApellidoPaternoUsuario]
        .filter(Boolean)
        .join(' ') ||
      prestamo.CarnetUsuario ||
      'Sin solicitante registrado'
    );
  }

  get estado(): string {
    return this.prestamoPrincipal?.EstadoPrestamo || 'Sin estado';
  }
}
