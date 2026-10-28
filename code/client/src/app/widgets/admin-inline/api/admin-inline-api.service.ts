import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { HistorialEquipoDto } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import {
  EquipoHistorialInlineItem,
  EquipoInlineItem,
  GaveteroInlineItem,
  PrestamoInlineItem,
} from '../model';

@Injectable({ providedIn: 'root' })
export class AdminInlineApiService {
  private readonly apiUrl = `${environment.apiUrl}/api`;

  constructor(private readonly http: HttpClient) {}

  obtenerEquiposPorGrupo(grupoId: number): Observable<EquipoInlineItem[]> {
    return this.obtenerLista('equipos', { grupoId });
  }

  obtenerEquiposPorGavetero(
    gaveteroId: number,
  ): Observable<EquipoInlineItem[]> {
    return this.obtenerLista('equipos', { gaveteroId });
  }

  obtenerGaveterosPorMueble(
    muebleId: number,
  ): Observable<GaveteroInlineItem[]> {
    return this.obtenerLista('gaveteros', { muebleId });
  }

  obtenerPrestamosPorCarnet(carnet: string): Observable<PrestamoInlineItem[]> {
    return this.obtenerLista('prestamos', { carnet });
  }

  obtenerHistorialEquipo(equipoId: number): Observable<HistorialEquipoDto[]> {
    return this.obtenerLista<EquipoHistorialInlineItem>(
      `equipos/${equipoId}/prestamos`,
    ).pipe(map((items) => items.map(toHistorialEquipo)));
  }

  private obtenerLista<T>(
    ruta: string,
    filtros: Record<string, string | number> = {},
  ): Observable<T[]> {
    return this.http
      .get<ApiResponse<T[]>>(`${this.apiUrl}/${ruta}`, {
        params: new HttpParams({ fromObject: filtros }),
      })
      .pipe(map((respuesta) => extractApiValue(respuesta, [])));
  }
}

function toHistorialEquipo(
  item: EquipoHistorialInlineItem,
): HistorialEquipoDto {
  return {
    IdPrestamo: item.IdPrestamo,
    NombreUsuario: item.NombreUsuario ?? undefined,
    Carnet: item.Carnet ?? undefined,
    EstadoPrestamo: item.EstadoPrestamo ?? undefined,
    EstadoEquipo: item.EstadoEquipo ?? undefined,
    Observacion: item.Observacion ?? undefined,
    FechaPrestamo: toDate(item.FechaPrestamo),
    FechaDevolucionEsperada: toDate(item.FechaDevolucionEsperada),
    FechaDevolucion: toDate(item.FechaDevolucion),
  };
}

function toDate(value: string | Date | null): Date | undefined {
  return value ? new Date(value) : undefined;
}
