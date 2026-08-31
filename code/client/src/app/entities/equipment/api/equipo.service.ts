import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Equipos } from '@entities/admin';
import { environment } from '@environments/environment';
import { map } from 'rxjs';
import { EquipoApiResponse } from './equipo-api-response';

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
      CostoReferencia: equipo.CostoReferencia,
      IdGavetero: equipo.IdGavetero,
      EstadoEquipo: equipo.EstadoEquipo,
      FechaIngresoEquipo: equipo.FechaIngresoEquipo,
    };

    return this.http.post<unknown>(this.apiUrl, envio);
  }

  obtenerEquipos() {
    return this.http.get<EquipoApiResponse>(this.apiUrl).pipe(
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
      CostoReferencia: equipo.CostoReferencia,
      IdGavetero: equipo.IdGavetero,
      FechaIngresoEquipo: equipo.FechaIngresoEquipo,
    };

    return this.http.put<unknown>(`${this.apiUrl}/${equipo.Id}`, envio);
  }

  eliminarEquipo(id: number) {
    return this.http.delete<unknown>(`${this.apiUrl}/${id}`);
  }
}
