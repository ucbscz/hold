import { HttpClient } from '@angular/common/http';
import { Injectable, signal, WritableSignal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ConfiguracionDto {
  MontoMinimoContrato: number;
  HorarioInicioMinutos: number;
  HorarioFinMinutos: number;
  NombreJefeCarrera: string;
  FirmaJefeCarreraBase64: string;
  TiempoMinimoReservaMinutos: number;
  TiempoRecordatorioPrevioMinutos: number;
  MinutosGraciaAtraso: number;
}

@Injectable({
  providedIn: 'root',
})
export class ConfiguracionService {
  private url = `${environment.apiUrl}/api/Configuracion`;
  
  public configuracionActual: WritableSignal<ConfiguracionDto | null> = signal(null);

  constructor(private http: HttpClient) {}

  public loadConfiguracion(): Observable<ConfiguracionDto> {
    return this.http.get<ConfiguracionDto>(this.url).pipe(
      tap((config) => {
        this.configuracionActual.set(config);
      })
    );
  }

  public updateConfiguracion(config: ConfiguracionDto): Observable<ConfiguracionDto> {
    return this.http.put<ConfiguracionDto>(this.url, config).pipe(
      tap((newConfig) => {
        this.configuracionActual.set(newConfig);
      })
    );
  }
}
