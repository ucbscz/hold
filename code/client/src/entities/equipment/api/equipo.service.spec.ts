import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { EquipoService } from './equipo.service';
describe('EquipoService', () => {
  let service: EquipoService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(EquipoService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
