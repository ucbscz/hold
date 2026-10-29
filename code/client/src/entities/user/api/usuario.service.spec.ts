import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { Usuario } from '../model/usuario';
import { UsuarioServiceAPI } from './usuario.service';

describe('UsuarioServiceAPI', () => {
  let service: UsuarioServiceAPI;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(UsuarioServiceAPI);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('resolves the selected career instead of sending the stale career id', () => {
    const usuario = new Usuario();
    usuario.carnet = '1234567';
    usuario.carrera = 'Ingeniería de Sistemas';
    usuario.carrera_Id = 1;

    service.editarUsuario(usuario, '').subscribe();

    const request = http.expectOne(
      (candidate) =>
        candidate.method === 'PUT' &&
        candidate.url.endsWith('/api/usuarios/1234567'),
    );
    expect(request.request.body.CarreraNombre).toBe('Ingeniería de Sistemas');
    expect(request.request.body.IdCarrera).toBeNull();
    request.flush({});
  });
});
