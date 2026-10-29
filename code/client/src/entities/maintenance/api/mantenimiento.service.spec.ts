import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { MantenimientoService } from './mantenimiento.service';
describe('MantenimientoService', () => {
  let service: MantenimientoService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(MantenimientoService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
