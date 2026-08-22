import { registerLocaleData } from '@angular/common';
import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import localeEs from '@angular/common/locales/es';
import {
  ApplicationConfig,
  LOCALE_ID,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { HttpCacheInterceptor } from '@app/providers/http-interceptors/http-cache.interceptor';
import { JwtInterceptor } from '@app/providers/http-interceptors/jwt.interceptor';
import { ResultResponseInterceptor } from '@app/providers/http-interceptors/result-response.interceptor';
import { routes } from '@app/routing/app.routes';

registerLocaleData(localeEs);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({
      eventCoalescing: true,
      runCoalescing: true,
    }),
    provideRouter(
      routes,
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled',
      }),
    ),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: LOCALE_ID, useValue: 'es' },
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ResultResponseInterceptor,
      multi: true,
    },
    { provide: HTTP_INTERCEPTORS, useClass: HttpCacheInterceptor, multi: true },
  ],
};
