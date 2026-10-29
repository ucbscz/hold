import { Component, Input } from '@angular/core';
import { Aviso } from '@shared/ui';
import type { FormularioAdminComponent } from './formulario-admin';

@Component({
  selector: 'app-formulario-admin-marco',
  imports: [Aviso],
  template: `
    <div class="form-container">
      <button class="btn-close" type="button" (click)="formulario.cerrar()">
        <img src="assets/x.png" alt="Cerrar" />
      </button>
      <div class="form-content">
        <h2>{{ formulario.textos.titulo }}</h2>
        <ng-content />
      </div>
    </div>
    @if (formulario.aviso()) {
      <app-aviso
        [cerrar]="formulario.aviso"
        [mensaje]="formulario.mensajeaviso"
        (aceptar)="formulario.confirmar()"
      ></app-aviso>
    }
  `,
  styleUrl: './formulario-admin-marco.css',
})
export class FormularioAdminMarcoComponent {
  @Input({ required: true }) formulario!: FormularioAdminComponent<object>;
}
