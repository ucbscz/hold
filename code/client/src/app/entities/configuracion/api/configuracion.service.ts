import { HttpClient } from '@angular/common/http';
import { Injectable, signal, WritableSignal } from '@angular/core';
import { catchError, Observable, of, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CONFIGURACION_PREDETERMINADA,
  ConfiguracionDto,
} from '../model/configuracion';

@Injectable({
  providedIn: 'root',
})
export class ConfiguracionService {
  private readonly url = `${environment.apiUrl}/api/configuracion`;

  readonly configuracionActual: WritableSignal<ConfiguracionDto | null> =
    signal(null);

  constructor(private readonly http: HttpClient) {}

  public loadConfiguracion(): Observable<ConfiguracionDto> {
    return this.http.get<ConfiguracionDto>(this.url).pipe(
      tap((config) => {
        this.configuracionActual.set(config);
      }),
      catchError(() => {
        const fallback = { ...CONFIGURACION_PREDETERMINADA };
        this.configuracionActual.set(fallback);
        return of(fallback);
      }),
    );
  }

  public updateConfiguracion(
    config: ConfiguracionDto,
  ): Observable<ConfiguracionDto> {
    return this.http.put<ConfiguracionDto>(this.url, config).pipe(
      tap((newConfig) => {
        this.configuracionActual.set(newConfig);
      }),
    );
  }
}
