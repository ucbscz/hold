import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ComponenteService } from './componente.service';
describe('ComponenteService', () => {
  let service: ComponenteService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(ComponenteService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
