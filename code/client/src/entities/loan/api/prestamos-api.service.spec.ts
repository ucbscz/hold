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
});
