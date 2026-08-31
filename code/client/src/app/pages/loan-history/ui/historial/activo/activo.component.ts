import { RecursosPrestamoComponent } from '@entities/loan';
import { CommonModule, DatePipe } from '@angular/common';
import { Component } from '@angular/core';
import { PrestamosAPIService, VistaPrestamosComponent } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { TablePaginationComponent } from '@shared/lib/admin-table';
import { AvisoExitoComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';
@Component({
  selector: 'app-activo',
  imports: [
    RecursosPrestamoComponent,
    CommonModule,
    DatePipe,
    VistaPrestamosComponent,
    AvisoExitoComponent,
    TablePaginationComponent,
  ],
  templateUrl: './activo.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class ActivoComponent extends HistorialBase {
  override estado: string = 'activo';
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
