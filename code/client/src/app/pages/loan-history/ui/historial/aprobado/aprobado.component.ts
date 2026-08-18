import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { PrestamoDto } from '@entities/admin';
import { PrestamosAPIService, VistaPrestamosComponent } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, AvisoExitoComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';
@Component({
  selector: 'app-aprobado',
  standalone: true,
  imports: [CommonModule, Aviso, VistaPrestamosComponent, AvisoExitoComponent],
  templateUrl: './aprobado.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class AprobadoComponent extends HistorialBase {
  override estado: string = 'aprobado';
  avisocancelar: WritableSignal<boolean> = signal(false);
  constructor(
    protected override usuario: UsuarioService,
    protected override prestamoApi: PrestamosAPIService,
  ) {
    super(prestamoApi, usuario);
  }

  ngOnInit() {
    this.cargarDatos();
  }

  abrirAvisoCancelacion(event: Event, item: PrestamoDto): void {
    event.stopPropagation();
    this.avisocancelar.set(true);
    this.itemSeleccionado = item;
  }

  cancelar() {
    this.prestamoApi
      .cambiarEstadoPrestamo(this.itemSeleccionado!.Id, 'cancelado')
      .subscribe({
        next: () => {
          this.cargarDatos();
          this.itemSeleccionado = null;
          this.avisocancelar.set(false);
          this.mensajeexito = 'Préstamo cancelado con éxito.';
          this.exito.set(true);
        },
        error: (error) => {
          const msg = extractErrorMessage(error);
          this.mensajeerror = `Error al cancelar el préstamo: ${msg}`;
          this.error.set(true);
        },
      });
  }
}
