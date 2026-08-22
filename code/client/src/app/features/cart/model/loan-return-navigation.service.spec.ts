import { Router } from '@angular/router';
import { BrowserSessionStorageService } from '@shared/lib/session';
import { LoanReturnNavigationService } from './loan-return-navigation.service';

describe('LoanReturnNavigationService', () => {
  let values: Map<string, string>;
  let storage: jasmine.SpyObj<BrowserSessionStorageService>;
  let router: jasmine.SpyObj<Router>;
  let service: LoanReturnNavigationService;

  beforeEach(() => {
    values = new Map<string, string>();
    storage = jasmine.createSpyObj<BrowserSessionStorageService>(
      'BrowserSessionStorageService',
      ['getItem', 'setItem', 'removeItem'],
    );
    storage.getItem.and.callFake((key) => values.get(key) ?? null);
    storage.setItem.and.callFake((key, value) => values.set(key, value));
    storage.removeItem.and.callFake((key) => values.delete(key));
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);
    router.navigateByUrl.and.resolveTo(true);
    service = new LoanReturnNavigationService(storage, router);
  });

  it('returns to the last page outside the loan flow', async () => {
    service.track('/administracion?seccion=prestamos');
    service.track('/carrito?step=2');
    service.track('/reserva');

    await service.returnToPreviousPage();

    expect(router.navigateByUrl).toHaveBeenCalledOnceWith(
      '/administracion?seccion=prestamos',
    );
  });

  it('returns to home after a loan started from home', async () => {
    service.track('/inicio');
    service.track('/carrito');

    await service.returnToPreviousPage();

    expect(router.navigateByUrl).toHaveBeenCalledOnceWith('/inicio');
  });

  it('uses home when no valid previous page exists', async () => {
    service.track('https://example.com');

    await service.returnToPreviousPage();

    expect(router.navigateByUrl).toHaveBeenCalledOnceWith('/inicio');
  });
});
