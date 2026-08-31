import {
  HttpHandler,
  HttpHeaders,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { of } from 'rxjs';
import { HttpCacheInterceptor } from './http-cache.interceptor';

describe('HttpCacheInterceptor', () => {
  for (const directive of ['no-store', 'private, no-cache']) {
    it(`does not reuse ${directive} configuration across requests`, () => {
      const cache = new HttpCacheInterceptor();
      const handle = jasmine.createSpy('handle').and.returnValue(
        of(
          new HttpResponse({
            body: { CarnetJefeCarrera: null },
            headers: new HttpHeaders({ 'Cache-Control': directive }),
          }),
        ),
      );
      const request = new HttpRequest('GET', '/api/configuracion');
      cache.intercept(request, { handle } as HttpHandler).subscribe();
      cache.intercept(request, { handle } as HttpHandler).subscribe();
      expect(handle).toHaveBeenCalledTimes(2);
    });
  }
  it('continues caching ordinary catalog responses and invalidates after edits', () => {
    const cache = new HttpCacheInterceptor();
    const handle = jasmine
      .createSpy('handle')
      .and.returnValue(of(new HttpResponse({ body: [] })));
    const handler = { handle } as HttpHandler;
    const request = new HttpRequest('GET', '/api/equipos');
    cache.intercept(request, handler).subscribe();
    cache.intercept(request, handler).subscribe();
    expect(handle).toHaveBeenCalledTimes(1);
    cache
      .intercept(new HttpRequest('PUT', '/api/equipos/1', {}), handler)
      .subscribe();
    cache.intercept(request, handler).subscribe();
    expect(handle).toHaveBeenCalledTimes(3);
  });
});
