import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { UsuarioService } from './usuario.service';
describe('UsuarioService', () => {
  let service: UsuarioService;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(UsuarioService);
  });
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
