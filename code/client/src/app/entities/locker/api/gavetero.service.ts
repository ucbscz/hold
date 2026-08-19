import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Gaveteros } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { GaveteroApiItem } from './gavetero-api-item';
@Injectable({
  providedIn: 'root',
})
export class GaveteroService {
  private readonly apiUrl = environment.apiUrl + '/api/gaveteros';
  constructor(private readonly http: HttpClient) {}

  crearGavetero(gavetero: Gaveteros) {
    const envio = {
      Nombre: gavetero.Nombre,
      Tipo: gavetero.Tipo,
      NombreMueble: gavetero.NombreMueble,
      Longitud: gavetero.Longitud,
      Profundidad: gavetero.Profundidad,
      Altura: gavetero.Altura,
    };
    return this.http.post<unknown>(this.apiUrl, envio);
  }

  obtenerGaveteros(): Observable<Gaveteros[]> {
    return this.http.get<ApiResponse<GaveteroApiItem[]>>(this.apiUrl).pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => ({
          Id: item.Id,
          Nombre: item.Nombre,
          Tipo: item.Tipo,
          NombreMueble: item.NombreMueble,
          Longitud: item.Longitud,
          Profundidad: item.Profundidad,
          Altura: item.Altura,
        })),
      ),
    );
  }

  editarGavetero(gavetero: Gaveteros) {
    const envio = {
      Id: gavetero.Id,
      Nombre: gavetero.Nombre,
      Tipo: gavetero.Tipo,
      NombreMueble: gavetero.NombreMueble,
      Longitud: gavetero.Longitud,
      Profundidad: gavetero.Profundidad,
      Altura: gavetero.Altura,
    };
    return this.http.put<unknown>(`${this.apiUrl}/${gavetero.Id}`, envio);
  }

  eliminarGavetero(id: number) {
    return this.http.delete<unknown>(`${this.apiUrl}/${id}`);
  }
}
