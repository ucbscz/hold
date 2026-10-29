import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CarreraService } from './carrera.service';
describe('CarreraService', () => {
  let service: CarreraService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(CarreraService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
