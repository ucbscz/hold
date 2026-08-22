import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { DisponibilidadService } from './disponibilidad.service';
describe('DisponibilidadService', () => {
  let service: DisponibilidadService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(DisponibilidadService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should split availability requests into backend-sized batches', () => {
    const ids = Array.from({ length: 101 }, (_, index) => index + 1);
    let resultLength = 0;

    service
      .obtenerDisponibilidad(
        new Date('2026-08-24T12:00:00Z'),
        new Date('2026-08-24T12:30:00Z'),
        ids,
      )
      .subscribe((result) => (resultLength = result.length));

    const requests = http.match((request) =>
      request.url.endsWith('/api/carrito/disponibilidad'),
    );
    expect(requests.length).toBe(2);
    expect(requests[0].request.body.ArrayIds.length).toBe(100);
    expect(requests[1].request.body.ArrayIds).toEqual([101]);

    requests[0].flush({ Value: [] });
    requests[1].flush({ Value: [] });
    expect(resultLength).toBe(0);
  });

  it('should share identical concurrent requests', () => {
    const inicio = new Date('2026-08-24T12:00:00Z');
    const fin = new Date('2026-08-24T12:30:00Z');

    service.obtenerDisponibilidad(inicio, fin, [1]).subscribe();
    service.obtenerDisponibilidad(inicio, fin, [1]).subscribe();

    const request = http.expectOne((candidate) =>
      candidate.url.endsWith('/api/carrito/disponibilidad'),
    );
    expect(request.request.method).toBe('POST');
    request.flush({ Value: [] });
  });
});
