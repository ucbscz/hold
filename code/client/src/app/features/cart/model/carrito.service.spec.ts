import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CarritoService } from './carrito.service';
describe('CarritoService', () => {
  let service: CarritoService;
  let totals: number[];

  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(CarritoService);
    totals = [];
    service.total$.subscribe((total) => totals.push(total));
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('removes all units when the product is removed from its detail view', () => {
    service.establecerCantidad(10, 'Multímetro', '', '', '', 0, 3, 2);

    service.eliminarProducto(10);

    expect(service.contieneProducto(10)).toBeFalse();
    expect(totals.at(-1)).toBe(0);
  });

  it('emits the total units whenever a cart quantity changes', () => {
    service.establecerCantidad(10, 'Multímetro', '', '', '', 0, 3, 1);
    service.editarCantidad(10, 3);
    service.editarCantidad(10, 2);

    expect(totals).toEqual([0, 1, 3, 2]);
  });

  it('counts a newly established cart item from its first unit', () => {
    service.establecerCantidad(10, 'Multimetro', '', '', '', 0, 3, 1);

    expect(totals).toEqual([0, 1]);
  });

  it('emits the exact requested quantity for a newly established item', () => {
    service.establecerCantidad(10, 'Multimetro', '', '', '', 0, 4, 3);

    expect(totals.at(-1)).toBe(3);
  });
});
