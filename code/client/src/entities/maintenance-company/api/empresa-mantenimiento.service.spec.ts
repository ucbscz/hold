import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { EmpresamantenimientoService } from './empresa-mantenimiento.service';
describe('EmpresamantenimientoService', () => {
  let service: EmpresamantenimientoService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule(withDefaultTestingProviders({}));
    service = TestBed.inject(EmpresamantenimientoService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('preserves the NIT when loading and editing a company', () => {
    service.obtenerEmpresaMantenimiento().subscribe((companies) => {
      const company = companies[0];
      expect(company.Nit).toBe('10203040');

      service.actualizarEmpresaMantenimiento(company).subscribe();
      const update = http.expectOne((request) =>
        request.url.endsWith('/api/empresas/7'),
      );
      expect(update.request.method).toBe('PUT');
      expect(update.request.body.Nit).toBe('10203040');
      update.flush({});
    });

    const list = http.expectOne((request) =>
      request.url.endsWith('/api/empresas'),
    );
    list.flush({
      Value: [
        {
          Id: 7,
          NombreEmpresa: 'Servicio técnico',
          NombreResponsable: 'Ana',
          ApellidoResponsable: 'Pérez',
          Telefono: '70000000',
          Nit: '10203040',
          Direccion: 'Av. Principal',
        },
      ],
    });
  });
});
