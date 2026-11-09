import { NgClass } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AriaOrden } from './orden-columna';

export interface OrdenColumnas {
  ordenarPorColumna(columna: string): void;
  iconoOrdenColumna(columna: string): string;
  ariaOrdenColumna(columna: string): AriaOrden;
}

@Component({
  selector: 'th[appColumnaOrdenable]',
  imports: [NgClass],
  host: {
    scope: 'col',
    '[attr.aria-sort]': 'tabla.ariaOrdenColumna(columna)',
  },
  template: `
    <button
      type="button"
      class="table-sort-button"
      (click)="tabla.ordenarPorColumna(columna)"
    >
      {{ columna }}
      <i
        class="fas"
        [ngClass]="tabla.iconoOrdenColumna(columna)"
        aria-hidden="true"
      ></i>
    </button>
  `,
})
export class ColumnaOrdenableComponent {
  @Input({ required: true, alias: 'appColumnaOrdenable' }) columna!: string;
  @Input({ required: true }) tabla!: OrdenColumnas;
}
