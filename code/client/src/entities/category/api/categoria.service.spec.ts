import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CategoriaService } from './categoria.service';
describe('CategoriaService', () => {
  let service: CategoriaService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(CategoriaService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
