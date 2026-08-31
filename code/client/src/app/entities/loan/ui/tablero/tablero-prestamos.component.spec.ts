import { TestBed } from '@angular/core/testing';
import { PrestamoDto } from '@entities/admin';
import { TableroPrestamosComponent } from './tablero-prestamos.component';

describe('TableroPrestamosComponent', () => {
  it('agrupa equipos por prestamo y muestra pendientes en la primera columna', () => {
    const component = new TableroPrestamosComponent();
    component.prestamos = [1, 1, 2].map((Id) =>
      Object.assign(new PrestamoDto(), {
        Id,
        EstadoPrestamo: Id === 1 ? 'pendiente' : 'activo',
      }),
    );
    component.ngOnChanges();
    expect(component.columnas[0].filas.length).toBe(1);
    expect(component.columnas[0].filas[0].length).toBe(2);
    expect(component.columnas[2].filas.length).toBe(1);
  });
  it('ofrece cancelar al propietario y no ofrece aprobar', async () => {
    await TestBed.configureTestingModule({
      imports: [TableroPrestamosComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(TableroPrestamosComponent);
    fixture.componentRef.setInput('prestamos', [
      Object.assign(new PrestamoDto(), { Id: 1, EstadoPrestamo: 'pendiente' }),
    ]);
    fixture.detectChanges();
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ) as HTMLButtonElement[];
    expect(buttons.some((b) => b.title === 'Aprobar préstamo')).toBeFalse();
    const accion = spyOn(fixture.componentInstance.accion, 'emit');
    buttons.find((b) => b.textContent?.trim() === 'Cancelar')!.click();
    expect(accion).toHaveBeenCalledWith({ id: 1, accion: 'cancelar' });
  });
});
