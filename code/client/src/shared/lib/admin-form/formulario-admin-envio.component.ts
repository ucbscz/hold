import { Component, Input } from '@angular/core';
import { MostrarerrorComponent } from '@shared/ui';
import type { FormularioAdminComponent } from './formulario-admin';

@Component({
  selector: 'app-formulario-admin-envio',
  imports: [MostrarerrorComponent],
  template: `
    @if (formulario.error()) {
      <app-mostrarerror
        [inline]="true"
        [dismissible]="descartable"
        [error]="formulario.error"
        [mensaje]="formulario.mensajeerror"
      ></app-mostrarerror>
    }
    <div class="form-submit">
      <button type="submit" class="register-button" [disabled]="deshabilitado">
        <span>{{ formulario.textos.boton }}</span>
      </button>
    </div>
  `,
  styleUrl: './formulario-admin-envio.css',
})
export class FormularioAdminEnvioComponent {
  @Input({ required: true }) formulario!: FormularioAdminComponent<object>;
  @Input({ required: true }) deshabilitado = false;
  @Input() descartable = true;
}
