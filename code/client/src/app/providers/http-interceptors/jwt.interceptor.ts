import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@entities/user';
import { Observable, throwError } from 'rxjs';
import { catchError, finalize, shareReplay, switchMap } from 'rxjs/operators';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  private refreshRequest$: Observable<void> | null = null;

  constructor(
    private readonly authService: AuthService,
    private router: Router,
  ) {}

  intercept(
    outgoingRequest: HttpRequest<unknown>,
    nextHandler: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    const requestWithCookies = outgoingRequest.clone({ withCredentials: true });
    const isSessionEndpoint =
      outgoingRequest.url.includes('/api/auth/login') ||
      outgoingRequest.url.includes('/api/auth/refresh') ||
      outgoingRequest.url.includes('/api/auth/logout') ||
      outgoingRequest.url.includes('/api/auth/externo/intercambiar');

    if (isSessionEndpoint) return nextHandler.handle(requestWithCookies);

    return nextHandler.handle(requestWithCookies).pipe(
      catchError((responseError) => {
        if (
          responseError instanceof HttpErrorResponse &&
          responseError.status === 401
        ) {
          return this.handleExpiredToken(
            requestWithCookies,
            nextHandler,
            responseError,
          );
        }
        return throwError(() => responseError);
      }),
    );
  }

  private handleExpiredToken(
    failedRequest: HttpRequest<unknown>,
    nextHandler: HttpHandler,
    originalError: HttpErrorResponse,
  ): Observable<HttpEvent<unknown>> {
    if (!this.authService.isLoggedIn()) {
      return throwError(() => originalError);
    }

    if (!this.refreshRequest$) {
      this.refreshRequest$ = this.authService.refreshSession().pipe(
        finalize(() => (this.refreshRequest$ = null)),
        shareReplay({ bufferSize: 1, refCount: true }),
      );
    }

    return this.refreshRequest$.pipe(
      switchMap(() => nextHandler.handle(failedRequest)),
      catchError((refreshError) => {
        this.authService.clear();
        this.router.navigate(['/login']);
        return throwError(() => refreshError);
      }),
    );
  }
}
