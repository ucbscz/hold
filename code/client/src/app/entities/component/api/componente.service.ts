import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Componente } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { ComponenteApiItem } from './componente-api-item';
@Injectable({
  providedIn: 'root',
})
export class ComponenteService {
  private readonly apiUrl = environment.apiUrl + '/api/componente';
  constructor(private readonly http: HttpClient) {}

  crearComponente(componente: Componente) {
    const envio = {
      Nombre: componente.Nombre,
      Modelo: componente.Modelo,
      Tipo: componente.Tipo,
      Descripcion: componente.Descripcion,
      PrecioReferencia: componente.PrecioReferencia,
      UrlDataSheet: componente.UrlDataSheet,
      IdEquipo: componente.IdEquipo || 0,
    };
    return this.http.post<unknown>(this.apiUrl, envio);
  }

  obtenerComponentes(): Observable<Componente[]> {
    return this.http.get<ApiResponse<ComponenteApiItem[]>>(this.apiUrl).pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => ({
          Id: item.Id,
          Nombre: item.Nombre,
          Modelo: item.Modelo,
          Tipo: item.Tipo,
          Descripcion: item.Descripcion,
          PrecioReferencia: item.PrecioReferencia,
          NombreEquipo: item.NombreEquipo,
          CodigoImtEquipo: item.CodigoImtEquipo,
          UrlDataSheet: item.UrlDataSheet,
          IdEquipo: item.IdEquipo,
        })),
      ),
    );
  }

  actualizarComponente(componente: Componente) {
    const envio = {
      Id: componente.Id,
      Nombre: componente.Nombre,
      Modelo: componente.Modelo,
      Tipo: componente.Tipo,
      Descripcion: componente.Descripcion,
      PrecioReferencia: componente.PrecioReferencia,
      UrlDataSheet: componente.UrlDataSheet,
      IdEquipo: componente.IdEquipo || 0,
    };
    return this.http.put<unknown>(`${this.apiUrl}/${componente.Id}`, envio);
  }

  eliminarComponente(id: number) {
    return this.http.delete<unknown>(this.apiUrl + '/' + id);
  }
}
