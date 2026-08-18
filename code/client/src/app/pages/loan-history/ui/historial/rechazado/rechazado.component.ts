import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PrestamosAPIService, VistaPrestamosComponent } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { MostrarerrorComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';
@Component({
  selector: 'app-rechazado',
  imports: [CommonModule, VistaPrestamosComponent, MostrarerrorComponent],
  templateUrl: './rechazado.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class RechazadoComponent extends HistorialBase {
  override estado: string = 'rechazado';
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
