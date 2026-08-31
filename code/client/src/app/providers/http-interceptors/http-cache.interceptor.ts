import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { CacheEntry } from './cache-entry';

@Injectable()
export class HttpCacheInterceptor implements HttpInterceptor {
  private readonly cache = new Map<string, CacheEntry>();
  private readonly ttlMs = 60_000;

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    if (request.method !== 'GET') {
      this.invalidate(request.url);

      return next.handle(request);
    }

    const key = request.urlWithParams;
    const hit = this.cache.get(key);

    if (hit && hit.expires > Date.now()) {
      return of(hit.event.clone());
    }

    if (hit) {
      this.cache.delete(key);
    }

    return next.handle(request).pipe(
      tap((event) => {
        if (event instanceof HttpResponse) {
          if (
            /\b(no-store|no-cache)\b/i.test(
              event.headers.get('Cache-Control') ?? '',
            )
          ) {
            this.cache.delete(key);
            return;
          }
          this.cache.set(key, {
            expires: Date.now() + this.ttlMs,
            event: event.clone(),
          });
        }
      }),
    );
  }

  private invalidate(url: string): void {
    const resource = this.resourceOf(url);

    for (const key of this.cache.keys()) {
      if (this.resourceOf(key) === resource) {
        this.cache.delete(key);
      }
    }
  }

  private resourceOf(url: string): string {
    const match = url.match(/\/api\/[^/?]+/i);
    return match ? match[0].toLowerCase() : url;
  }
}
