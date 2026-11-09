import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Equipos } from '../model/equipo';
import { environment } from '@shared/config';
import {
  getApiList,
  obtenerTodasLasPaginas,
  parametrosPagina,
} from '@shared/api';
import { map, Observable } from 'rxjs';
import { EquipoHistorialInlineItem } from '../model/equipo-historial-inline-item';
import { EquipoInlineItem } from '../model/equipo-inline-item';
import { HistorialEquipoDto } from '../model/historial-equipo';
import { EquipoApiResponse } from './equipo-api-response';
import { toHistorialEquipo } from './historial-equipo.mapper';

@Injectable({
  providedIn: 'root',
})
export class EquipoService {
  private readonly apiUrl = environment.apiUrl + '/api/equipos';

  constructor(private readonly http: HttpClient) {}

  crearEquipo(equipo: Equipos) {
    const envio = {
      IdGrupoEquipo: equipo.IdGrupoEquipo,
      CodigoUcb: equipo.CodigoUcb,
      Descripcion: equipo.Descripcion,
      NumeroSerial: equipo.NumeroSerial,
      IdAmbiente: equipo.IdAmbiente ?? null,
      IdProcedencia: equipo.IdProcedencia ?? null,
      IdCarrera: equipo.IdCarrera ?? null,
      CostoReferencia: equipo.CostoReferencia,
      IdGavetero: equipo.IdGavetero,
      EstadoEquipo: equipo.EstadoEquipo,
      FechaIngresoEquipo: equipo.FechaIngresoEquipo,
    };

    return this.http.post<unknown>(this.apiUrl, envio);
  }

  obtenerEquipos() {
    return obtenerTodasLasPaginas((pagina) =>
      this.obtenerPaginaEquipos(pagina),
    );
  }

  private obtenerPaginaEquipos(pagina: number) {
    return this.http
      .get<EquipoApiResponse>(this.apiUrl, { params: parametrosPagina(pagina) })
      .pipe(
        map((data) =>
          data.Value.map((item) => ({
            Id: item.Id,
            NombreGrupoEquipo: item.NombreGrupoEquipo,
            IdGrupoEquipo: item.IdGrupoEquipo,
            CodigoImt: item.CodigoImt,
            CodigoUcb: item.CodigoUcb,
            NumeroSerial: item.NumeroSerial,
            EstadoEquipo: item.EstadoEquipo,
            Ubicacion: item.Ubicacion,
            IdAmbiente: item.IdAmbiente,
            IdProcedencia: item.IdProcedencia,
            NombreGavetero: item.NombreGavetero,
            IdGavetero: item.IdGavetero,
            CostoReferencia: item.CostoReferencia,
            Descripcion: item.Descripcion,
            Procedencia: item.Procedencia,
            IdCarrera: item.IdCarrera ?? null,
            CodigoCarrera: item.CodigoCarrera ?? null,
            FechaIngresoEquipo: item.FechaIngresoEquipo,
          })),
        ),
      );
  }

  editarEquipo(equipo: Equipos) {
    const envio = {
      Id: equipo.Id,
      IdGrupoEquipo: equipo.IdGrupoEquipo,
      CodigoUcb: equipo.CodigoUcb,
      Descripcion: equipo.Descripcion,
      EstadoEquipo: equipo.EstadoEquipo,
      NumeroSerial: equipo.NumeroSerial,
      IdAmbiente: equipo.IdAmbiente ?? null,
      IdProcedencia: equipo.IdProcedencia ?? null,
      IdCarrera: equipo.IdCarrera ?? null,
      CostoReferencia: equipo.CostoReferencia,
      IdGavetero: equipo.IdGavetero,
      FechaIngresoEquipo: equipo.FechaIngresoEquipo,
    };

    return this.http.put<unknown>(`${this.apiUrl}/${equipo.Id}`, envio);
  }

  obtenerEquiposPorGrupo(grupoId: number): Observable<EquipoInlineItem[]> {
    return getApiList(this.http, this.apiUrl, { grupoId });
  }

  obtenerEquiposPorGavetero(
    gaveteroId: number,
  ): Observable<EquipoInlineItem[]> {
    return getApiList(this.http, this.apiUrl, { gaveteroId });
  }

  obtenerHistorialEquipo(equipoId: number): Observable<HistorialEquipoDto[]> {
    return getApiList<EquipoHistorialInlineItem>(
      this.http,
      `${this.apiUrl}/${equipoId}/prestamos`,
    ).pipe(map((items) => items.map(toHistorialEquipo)));
  }

  eliminarEquipo(id: number) {
    return this.http.delete<unknown>(`${this.apiUrl}/${id}`);
  }
}
