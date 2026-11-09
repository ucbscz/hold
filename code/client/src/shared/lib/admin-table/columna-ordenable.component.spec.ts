import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ColumnaOrdenableComponent } from './columna-ordenable.component';
import {
  AriaOrden,
  ariaOrden,
  iconoOrden,
  siguienteDireccion,
} from './orden-columna';

@Component({
  imports: [ColumnaOrdenableComponent],
  template: `<table>
    <thead>
      <tr>
        <th appColumnaOrdenable="Nombre" [tabla]="this"></th>
      </tr>
    </thead>
  </table>`,
})
class TablaPruebaComponent {
  columna = '';
  direccion: 'asc' | 'desc' = 'asc';

  ordenarPorColumna(columna: string): void {
    this.direccion = siguienteDireccion(this.columna, this.direccion, columna);
    this.columna = columna;
  }

  iconoOrdenColumna(columna: string): string {
    return iconoOrden(this.columna === columna, this.direccion);
  }

  ariaOrdenColumna(columna: string): AriaOrden {
    return ariaOrden(this.columna === columna, this.direccion);
  }
}

describe('ColumnaOrdenableComponent', () => {
  it('toggles the sort state of its column from the header button', () => {
    const fixture = TestBed.createComponent(TablaPruebaComponent);
    fixture.detectChanges();
    const th: HTMLElement = fixture.nativeElement.querySelector('th');
    const boton: HTMLButtonElement = th.querySelector('button')!;

    expect(th.getAttribute('scope')).toBe('col');
    expect(th.getAttribute('aria-sort')).toBe('none');
    expect(boton.textContent?.trim()).toBe('Nombre');

    boton.click();
    fixture.detectChanges();
    expect(th.getAttribute('aria-sort')).toBe('ascending');
    expect(th.querySelector('i')!.classList).toContain('fa-sort-up');

    boton.click();
    fixture.detectChanges();
    expect(th.getAttribute('aria-sort')).toBe('descending');
    expect(th.querySelector('i')!.classList).toContain('fa-sort-down');
  });

  it('restarts ascending when another column is selected', () => {
    expect(siguienteDireccion('Nombre', 'asc', 'Nombre')).toBe('desc');
    expect(siguienteDireccion('Nombre', 'desc', 'Nombre')).toBe('asc');
    expect(siguienteDireccion('Nombre', 'asc', 'Tipo')).toBe('asc');
    expect(iconoOrden(false, 'desc')).toBe('fa-sort');
  });
});
