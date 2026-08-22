import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { finalize, forkJoin, map, Observable, of, shareReplay } from 'rxjs';
import { Disponibilidad } from '../model/disponibilidad';
import { DisponibilidadApiItem } from './disponibilidad-api-item';
import { DisponibilidadApiResponse } from './disponibilidad-api-response';
@Injectable({
  providedIn: 'root',
})
export class DisponibilidadService {
  private static readonly MAX_GROUPS_PER_REQUEST = 100;
  private readonly url = environment.apiUrl + '/api/carrito/disponibilidad';
  private readonly solicitudesEnCurso = new Map<
    string,
    Observable<Disponibilidad[]>
  >();

  constructor(private readonly http: HttpClient) {}
  private mapear(item: DisponibilidadApiItem): Disponibilidad {
    return {
      Fecha: item.Fecha ? new Date(item.Fecha) : null,
      IdGrupoEquipo: item.IdGrupoEquipo,
      CantidadDisponible: item.CantidadDisponible,
      TotalOperativo: item.TotalOperativo ?? 0,
    } as Disponibilidad;
  }
  obtenerDisponibilidad(
    fechaInicio: Date,
    fechaFin: Date,
    grupoEquipoIds: number[],
  ): Observable<Disponibilidad[]> {
    const ids = [...new Set(grupoEquipoIds)].filter(
      (id) => Number.isInteger(id) && id > 0,
    );
    if (ids.length === 0) return of([]);

    const inicioIso = fechaInicio.toISOString();
    const finIso = fechaFin.toISOString();
    const cacheKey = `${inicioIso}|${finIso}|${ids.join(',')}`;
    const solicitudExistente = this.solicitudesEnCurso.get(cacheKey);
    if (solicitudExistente) return solicitudExistente;

    const lotes = this.dividirEnLotes(
      ids,
      DisponibilidadService.MAX_GROUPS_PER_REQUEST,
    );
    const solicitud = forkJoin(
      lotes.map((lote) => this.consultarLote(inicioIso, finIso, lote)),
    ).pipe(
      map((resultados) => resultados.flat()),
      finalize(() => this.solicitudesEnCurso.delete(cacheKey)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    this.solicitudesEnCurso.set(cacheKey, solicitud);
    return solicitud;
  }

  private consultarLote(
    fechaInicio: string,
    fechaFin: string,
    ids: number[],
  ): Observable<Disponibilidad[]> {
    return this.http
      .post<DisponibilidadApiResponse | DisponibilidadApiItem[]>(this.url, {
        FechaInicio: fechaInicio,
        FechaFin: fechaFin,
        ArrayIds: ids,
      })
      .pipe(
        map((response) => {
          const data = Array.isArray(response)
            ? response
            : (response.Value ?? response.value ?? response.data ?? []);
          return data.map((item) => this.mapear(item));
        }),
      );
  }

  private dividirEnLotes(ids: number[], maximo: number): number[][] {
    const lotes: number[][] = [];
    for (let inicio = 0; inicio < ids.length; inicio += maximo) {
      lotes.push(ids.slice(inicio, inicio + maximo));
    }
    return lotes;
  }
}
