import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CarritoService } from './carrito.service';
describe('CarritoService', () => {
  let service: CarritoService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(CarritoService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('removes all units when the product is removed from its detail view', () => {
    service.agregarProducto(10, 'Multímetro', '', '', '', 0, 3);
    service.agregarProducto(10, 'Multímetro', '', '', '', 0, 3);

    service.eliminarProducto(10);

    expect(service.contieneProducto(10)).toBeFalse();
    expect(service.obtenerTotal()).toBe(0);
  });

  it('emits the total units whenever a cart quantity changes', () => {
    const totals: number[] = [];
    const subscription = service.total$.subscribe((total) =>
      totals.push(total),
    );

    service.agregarProducto(10, 'Multímetro', '', '', '', 0, 3);
    service.editarCantidad(10, 3);
    service.quitarProducto(10);

    expect(totals).toEqual([0, 1, 3, 2]);
    subscription.unsubscribe();
  });
});
