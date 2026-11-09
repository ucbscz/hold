import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { UsuarioService } from './usuario.service';
import { environment } from '@shared/config';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let sesion: UsuarioService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule(withDefaultTestingProviders());
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    sesion = TestBed.inject(UsuarioService);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('stores the user without browser-readable tokens', () => {
    sesion.guardarSesion({ nombre: 'Ana', rol: 'docente' });

    expect(service.isLoggedIn()).toBeTrue();
    expect(service.getRole()).toBe('docente');
    expect(sessionStorage.length).toBe(1);
  });

  it('refreshes using only the HttpOnly cookie', () => {
    service.refreshSession().subscribe();

    const request = http.expectOne(`${environment.apiUrl}/api/auth/refresh`);
    expect(request.request.withCredentials).toBeTrue();
    expect(request.request.body).toEqual({});
    request.flush({ Value: { Usuario: {} } });
  });

  it('clears local session data after logout', () => {
    sesion.guardarSesion({ nombre: 'Ana', rol: 'docente' });
    service.logout().subscribe();

    const request = http.expectOne(`${environment.apiUrl}/api/auth/logout`);
    expect(request.request.withCredentials).toBeTrue();
    request.flush(null);
    expect(service.isLoggedIn()).toBeFalse();
  });
});
