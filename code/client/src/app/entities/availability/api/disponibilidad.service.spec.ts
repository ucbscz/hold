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

  it('should request a whole calendar in one batch and preserve local dates', () => {
    let result: { Fecha: Date; Disponible: boolean }[] = [];

    service
      .obtenerDisponibilidadCalendario(
        new Date(2026, 8, 3, 9),
        new Date(2026, 8, 3, 9, 30),
        new Date(2026, 8, 1),
        new Date(2026, 8, 30),
        [{ idGrupoEquipo: 7, cantidad: 2 }],
      )
      .subscribe((value) => (result = value));

    const request = http.expectOne((candidate) =>
      candidate.url.endsWith('/api/carrito/disponibilidad/calendario'),
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body.Grupos).toEqual([
      { IdGrupoEquipo: 7, Cantidad: 2 },
    ]);
    request.flush({
      Value: [
        { Fecha: '2026-09-03T00:00:00Z', Disponible: false },
        { Fecha: '2026-09-04T00:00:00Z', Disponible: true },
      ],
    });

    expect(result.map((item) => item.Fecha.getDate())).toEqual([3, 4]);
    expect(result.map((item) => item.Disponible)).toEqual([false, true]);
  });
});
