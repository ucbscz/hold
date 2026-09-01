import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { PrestamosAPIService } from './prestamos-api.service';
describe('PrestamosAPIService', () => {
  let service: PrestamosAPIService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(PrestamosAPIService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('loads the authenticated institutional signer for the contract', () => {
    const signer = {
      Nombre: 'Ana Pérez',
      Carnet: '12890061',
      FirmaBase64: 'data:image/png;base64,aGVsbG8=',
    };
    const received = jasmine.createSpy('received');

    service.obtenerFirmanteContrato().subscribe(received);
    const request = http.expectOne((candidate) =>
      candidate.url.endsWith('/api/contratos/firmante'),
    );
    expect(request.request.method).toBe('GET');
    request.flush(signer);

    expect(received).toHaveBeenCalledOnceWith(signer);
  });
});
