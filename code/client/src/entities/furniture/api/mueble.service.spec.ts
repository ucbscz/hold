import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { MuebleService } from './mueble.service';
describe('MuebleService', () => {
  let service: MuebleService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(MuebleService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
