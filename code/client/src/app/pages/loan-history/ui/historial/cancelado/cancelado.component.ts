import { RecursosPrestamoComponent } from '@entities/loan';
import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PrestamosAPIService, VistaPrestamosComponent } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { TablePaginationComponent } from '@shared/lib/admin-table';
import { MostrarerrorComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';
@Component({
  selector: 'app-cancelado',
  imports: [
    RecursosPrestamoComponent,
    CommonModule,
    VistaPrestamosComponent,
    MostrarerrorComponent,
    TablePaginationComponent,
  ],
  templateUrl: './cancelado.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class CanceladoComponent extends HistorialBase {
  override estado = 'cancelado';
  constructor(
    protected override usuario: UsuarioService,
    protected override prestamoApi: PrestamosAPIService,
  ) {
    super(prestamoApi, usuario);
  }
  ngOnInit() {
    this.cargarDatos();
  }
}
