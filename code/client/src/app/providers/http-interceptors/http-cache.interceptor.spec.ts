import {
  HttpContext,
  HttpHandler,
  HttpHeaders,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { CacheHttpStore, CONSULTA_SIN_CAMBIOS } from '@shared/api';
import { of, Subject } from 'rxjs';
import { HttpCacheInterceptor } from './http-cache.interceptor';

describe('HttpCacheInterceptor', () => {
  const crear = () => new HttpCacheInterceptor(new CacheHttpStore());
  const manejador = () => {
    const handle = jasmine
      .createSpy('handle')
      .and.callFake(() => of(new HttpResponse({ body: [] })));
    return { handle, handler: { handle } as HttpHandler };
  };

  for (const directive of ['no-store', 'private, no-cache']) {
    it(`does not reuse ${directive} configuration across requests`, () => {
      const cache = crear();
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
    const cache = crear();
    const { handle, handler } = manejador();
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

  it('refreshes related tables when another resource changes', () => {
    const cache = crear();
    const { handle, handler } = manejador();
    const grupos = new HttpRequest('GET', '/api/grupos');
    cache.intercept(grupos, handler).subscribe();
    cache
      .intercept(new HttpRequest('POST', '/api/equipos', {}), handler)
      .subscribe();
    cache.intercept(grupos, handler).subscribe();
    expect(handle).toHaveBeenCalledTimes(3);
  });

  it('does not store a read that was in flight while data changed', () => {
    const cache = crear();
    const respuesta = new Subject<HttpResponse<unknown>>();
    const handle = jasmine
      .createSpy('handle')
      .and.returnValues(
        respuesta,
        of(new HttpResponse({ body: {} })),
        of(new HttpResponse({ body: [] })),
      );
    const handler = { handle } as HttpHandler;
    const lista = new HttpRequest('GET', '/api/usuarios');
    cache.intercept(lista, handler).subscribe();
    cache
      .intercept(new HttpRequest('DELETE', '/api/usuarios/1'), handler)
      .subscribe();
    respuesta.next(new HttpResponse({ body: ['viejo'] }));
    respuesta.complete();
    cache.intercept(lista, handler).subscribe();
    expect(handle).toHaveBeenCalledTimes(3);
  });

  it('keeps the cache for read-only queries sent by POST', () => {
    const cache = crear();
    const { handle, handler } = manejador();
    const grupos = new HttpRequest('GET', '/api/grupos');
    cache.intercept(grupos, handler).subscribe();
    cache
      .intercept(
        new HttpRequest(
          'POST',
          '/api/carrito/disponibilidad',
          {},
          { context: new HttpContext().set(CONSULTA_SIN_CAMBIOS, true) },
        ),
        handler,
      )
      .subscribe();
    cache.intercept(grupos, handler).subscribe();
    expect(handle).toHaveBeenCalledTimes(2);
  });
});
