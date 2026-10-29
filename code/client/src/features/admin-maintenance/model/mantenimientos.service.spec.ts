import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { MantenimientosServiceEquipos } from './mantenimientos-equipos.service';
describe('MantenimientosService', () => {
  let service: MantenimientosServiceEquipos;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(MantenimientosServiceEquipos);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
