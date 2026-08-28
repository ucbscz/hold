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
