import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AccesoriosService } from './accesorios.service';
describe('AccesoriosService', () => {
  let service: AccesoriosService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(AccesoriosService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
