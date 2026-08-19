import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { map } from 'rxjs';
import { Disponibilidad } from '../model/disponibilidad';
import { DisponibilidadApiItem } from './disponibilidad-api-item';
import { DisponibilidadApiResponse } from './disponibilidad-api-response';
@Injectable({
  providedIn: 'root',
})
export class DisponibilidadService {
  private readonly url = environment.apiUrl + '/api/carrito/disponibilidad';
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
  ) {
    const payload = {
      FechaInicio: fechaInicio.toISOString(),
      FechaFin: fechaFin.toISOString(),
      ArrayIds: grupoEquipoIds,
    };
    return this.http
      .post<DisponibilidadApiResponse | DisponibilidadApiItem[]>(
        this.url,
        payload,
      )
      .pipe(
        map((response) => {
          const data = Array.isArray(response)
            ? response
            : (response.Value ?? response.value ?? response.data ?? []);

          return data.map((item) => this.mapear(item));
        }),
      );
  }
}
