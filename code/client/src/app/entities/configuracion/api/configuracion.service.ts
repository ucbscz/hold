import { HttpClient } from '@angular/common/http';
import { Injectable, signal, WritableSignal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { ApiResponse, extractApiValue } from '@shared/api';
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

  public loadConfiguracion(useFallback = true): Observable<ConfiguracionDto> {
    return this.http.get<ConfiguracionDto>(this.url).pipe(
      tap((config) => {
        this.configuracionActual.set(config);
      }),
      catchError((error) => {
        if (!useFallback) throw error;
        const fallback = { ...CONFIGURACION_PREDETERMINADA };
        this.configuracionActual.set(fallback);
        return of(fallback);
      }),
    );
  }

  buscarResponsables(
    buscar = '',
  ): Observable<{ Carnet: string; Nombre: string }[]> {
    return this.http.get<{ Carnet: string; Nombre: string }[]>(
      `${this.url}/responsables`,
      {
        params: { buscar },
      },
    );
  }

  public updateConfiguracion(
    config: ConfiguracionDto,
  ): Observable<ConfiguracionDto> {
    return this.http
      .put<ApiResponse<ConfiguracionDto> | ConfiguracionDto>(this.url, config)
      .pipe(
        map((response) => extractApiValue(response, config)),
        tap((newConfig) => {
          this.configuracionActual.set(newConfig);
        }),
      );
  }
}
