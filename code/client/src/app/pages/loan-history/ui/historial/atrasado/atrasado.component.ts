import { CommonModule, DatePipe } from '@angular/common';
import { Component } from '@angular/core';
import { PrestamosAPIService, VistaPrestamosComponent } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { AvisoExitoComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';

@Component({
  selector: 'app-atrasado',
  imports: [
    CommonModule,
    DatePipe,
    VistaPrestamosComponent,
    AvisoExitoComponent,
  ],
  templateUrl: './atrasado.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class AtrasadoComponent extends HistorialBase {
  override estado: string = 'atrasado';
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
