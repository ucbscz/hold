import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import {
  CONFIGURACION_PREDETERMINADA,
  ConfiguracionDto,
} from '../model/configuracion';
import { ConfiguracionService } from './configuracion.service';

describe('ConfiguracionService', () => {
  let service: ConfiguracionService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(ConfiguracionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('unwraps the server response after saving and keeps the assigned user', () => {
    const config = {
      ...CONFIGURACION_PREDETERMINADA,
      CarnetJefeCarrera: '123',
      NombreJefeCarrera: 'Ana Perez',
    };
    let result: ConfiguracionDto | undefined;
    service.updateConfiguracion(config).subscribe((value) => (result = value));
    http
      .expectOne(
        (request) =>
          request.method === 'PUT' &&
          request.url.endsWith('/api/configuracion'),
      )
      .flush({ Status: 200, Value: config });
    expect(result).toEqual(config);
    expect(service.configuracionActual()).toEqual(config);
  });

  it('surfaces failures when editing configuration instead of returning defaults', () => {
    const error = jasmine.createSpy('error');
    service.loadConfiguracion(false).subscribe({ error });
    http
      .expectOne((request) => request.url.endsWith('/api/configuracion'))
      .flush('Unavailable', { status: 503, statusText: 'Unavailable' });
    expect(error).toHaveBeenCalled();
    expect(service.configuracionActual()).toBeNull();
  });

  it('searches responsible users using the documented query parameter', () => {
    const usuarios = [{ Carnet: '123', Nombre: 'Ana Perez' }];
    const recibido = jasmine.createSpy('recibido');
    service.buscarResponsables('Ana Perez').subscribe(recibido);
    const request = http.expectOne(
      (request) =>
        request.url.endsWith('/api/configuracion/responsables') &&
        request.params.get('buscar') === 'Ana Perez',
    );
    expect(request.request.method).toBe('GET');
    request.flush(usuarios);
    expect(recibido).toHaveBeenCalledOnceWith(usuarios);
  });

  it('should store the configuration returned by the API', () => {
    const configuracion: ConfiguracionDto = {
      ...CONFIGURACION_PREDETERMINADA,
      MontoMinimoContrato: 3500,
    };
    let result: ConfiguracionDto | undefined;

    service.loadConfiguracion().subscribe((value) => (result = value));
    http
      .expectOne((request) => request.url.endsWith('/api/configuracion'))
      .flush(configuracion);

    expect(result).toEqual(configuracion);
    expect(service.configuracionActual()).toEqual(configuracion);
  });

  it('should use safe defaults when the API is unavailable', () => {
    let result: ConfiguracionDto | undefined;

    service.loadConfiguracion().subscribe((value) => (result = value));
    http
      .expectOne((request) => request.url.endsWith('/api/configuracion'))
      .flush('Unavailable', { status: 503, statusText: 'Unavailable' });

    expect(result).toEqual(CONFIGURACION_PREDETERMINADA);
    expect(service.configuracionActual()).toEqual(CONFIGURACION_PREDETERMINADA);
  });
});
