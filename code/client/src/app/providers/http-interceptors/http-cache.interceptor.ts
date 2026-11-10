import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CacheHttpStore, CONSULTA_SIN_CAMBIOS } from '@shared/api';
import { Observable, of } from 'rxjs';
import { finalize, tap } from 'rxjs/operators';

@Injectable()
export class HttpCacheInterceptor implements HttpInterceptor {
  constructor(private readonly cache: CacheHttpStore) {}

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    if (request.method !== 'GET') {
      if (request.context.get(CONSULTA_SIN_CAMBIOS))
        return next.handle(request);

      this.cache.invalidar();

      return next.handle(request).pipe(finalize(() => this.cache.invalidar()));
    }

    const key = request.urlWithParams;
    const hit = this.cache.obtener(key);

    if (hit) return of(hit);

    const version = this.cache.version;

    return next.handle(request).pipe(
      tap((event) => {
        if (!(event instanceof HttpResponse)) return;

        if (
          /\b(no-store|no-cache)\b/i.test(
            event.headers.get('Cache-Control') ?? '',
          )
        ) {
          this.cache.eliminar(key);
          return;
        }

        this.cache.guardar(key, event, version);
      }),
    );
  }
}
