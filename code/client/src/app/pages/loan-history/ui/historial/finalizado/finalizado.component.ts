import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PrestamosAPIService, VistaPrestamosComponent } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { MostrarerrorComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';
@Component({
  selector: 'app-finalizado',
  imports: [CommonModule, VistaPrestamosComponent, MostrarerrorComponent],
  templateUrl: './finalizado.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class FinalizadoComponent extends HistorialBase {
  override estado: string = 'finalizado';
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
