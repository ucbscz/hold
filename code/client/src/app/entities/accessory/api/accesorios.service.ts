import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Accesorio } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { AccesorioApiItem } from './accesorio-api-item';
@Injectable({
  providedIn: 'root',
})
export class AccesoriosService {
  private readonly apiUrl = environment.apiUrl + '/api/accesorio';

  constructor(private readonly http: HttpClient) {}

  obtenerAccesorios(): Observable<Accesorio[]> {
    return this.http.get<ApiResponse<AccesorioApiItem[]>>(this.apiUrl).pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => ({
          Id: item.Id,
          nombre: item.Nombre,
          modelo: item.Modelo,
          tipo: item.Tipo,
          descripcion: item.Descripcion,
          codigo_imt: item.CodigoImtEquipoAsociado,
          precio: item.Precio,
          url_data_sheet: item.UrlDataSheet,
          id_equipo: item.IdEquipo,
          nombreEquipoAsociado: item.NombreEquipoAsociado,
        })),
      ),
    );
  }

  crearAccesorio(accesorio: Accesorio) {
    const envio = {
      Nombre: accesorio.nombre,
      Modelo: accesorio.modelo,
      Tipo: accesorio.tipo,
      Descripcion: accesorio.descripcion,
      Precio: accesorio.precio,
      UrlDataSheet: accesorio.url_data_sheet,
      IdEquipo: accesorio.id_equipo,
    };
    return this.http.post<unknown>(this.apiUrl, envio);
  }

  eliminarAccesorio(id: number) {
    return this.http.delete<unknown>(`${this.apiUrl}/${id}`);
  }

  editarAccesorio(accesorio: Accesorio) {
    const envio = {
      Id: accesorio.Id,
      Nombre: accesorio.nombre,
      Modelo: accesorio.modelo,
      Tipo: accesorio.tipo,
      Descripcion: accesorio.descripcion,
      Precio: accesorio.precio,
      UrlDataSheet: accesorio.url_data_sheet,
      IdEquipo: accesorio.id_equipo,
    };
    return this.http.put<unknown>(`${this.apiUrl}/${accesorio.Id}`, envio);
  }
}
