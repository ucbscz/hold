import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { GrupoequipoService } from './grupo-equipo.service';
describe('GrupoequipoService', () => {
  let service: GrupoequipoService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(GrupoequipoService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should reuse catalog data when opening equipment details', () => {
    let detailName: string | null | undefined;

    service.getGrupoEquipo('', '').subscribe();
    http
      .expectOne((request) => request.url.endsWith('/api/grupos'))
      .flush({
        Value: [{ Id: 7, Nombre: 'Osciloscopio', Cantidad: 1 }],
      });

    service.getproducto('7').subscribe((grupo) => (detailName = grupo.nombre));

    http.expectNone((request) => request.url.endsWith('/api/grupos/7'));
    expect(detailName).toBe('Osciloscopio');
  });

  it('should share concurrent detail requests', () => {
    service.getproducto('8').subscribe();
    service.getproducto('8').subscribe();

    const request = http.expectOne((candidate) =>
      candidate.url.endsWith('/api/grupos/8'),
    );
    expect(request.request.method).toBe('GET');
    request.flush({ Value: { Id: 8, Nombre: 'Multímetro' } });
  });
});
