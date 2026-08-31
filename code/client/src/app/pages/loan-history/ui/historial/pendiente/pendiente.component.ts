import { RecursosPrestamoComponent } from '@entities/loan';
import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { PrestamoDto } from '@entities/admin';
import { PrestamosAPIService, VistaPrestamosComponent } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';
import { TablePaginationComponent } from '@shared/lib/admin-table';
import { Aviso, AvisoExitoComponent, MostrarerrorComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';
@Component({
  selector: 'app-pendiente',
  imports: [
    RecursosPrestamoComponent,
    CommonModule,
    Aviso,
    VistaPrestamosComponent,
    MostrarerrorComponent,
    AvisoExitoComponent,
    TablePaginationComponent,
  ],
  templateUrl: './pendiente.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class PendienteComponent extends HistorialBase {
  override estado: string = 'pendiente';
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
    this.itemSeleccionado = item;
    this.avisocancelar.set(true);
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
