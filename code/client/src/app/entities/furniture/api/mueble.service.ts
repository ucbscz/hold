import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Muebles } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { MuebleApiItem } from './mueble-api-item';
@Injectable({
  providedIn: 'root',
})
export class MuebleService {
  private readonly apiUrl = environment.apiUrl + '/api/muebles';
  constructor(private readonly http: HttpClient) {}

  crearMueble(mueble: Muebles) {
    const enviar = {
      Nombre: mueble.Nombre,
      Tipo: mueble.Tipo,
      Costo: mueble.Costo,
      Ubicacion: mueble.Ubicacion,
      IdAmbiente: mueble.IdAmbiente,
      Longitud: mueble.Longitud,
      Profundidad: mueble.Profundidad,
      Altura: mueble.Altura,
    };
    return this.http.post<unknown>(this.apiUrl, enviar);
  }

  obtenerMuebles(): Observable<Muebles[]> {
    return this.http.get<ApiResponse<MuebleApiItem[]>>(this.apiUrl).pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => ({
          Id: item.Id,
          Nombre: item.Nombre,
          NumeroGaveteros: item.NumeroGaveteros,
          Ubicacion: item.Ubicacion,
          IdAmbiente: item.IdAmbiente ?? null,
          NombreAmbiente: item.NombreAmbiente ?? null,
          Tipo: item.Tipo,
          Costo: item.Costo,
          Longitud: item.Longitud,
          Profundidad: item.Profundidad,
          Altura: item.Altura,
        })),
      ),
    );
  }

  actualizarMueble(mueble: Muebles) {
    const enviar = {
      Id: mueble.Id,
      Nombre: mueble.Nombre,
      Tipo: mueble.Tipo,
      Costo: mueble.Costo,
      Ubicacion: mueble.Ubicacion,
      IdAmbiente: mueble.IdAmbiente,
      Longitud: mueble.Longitud,
      Profundidad: mueble.Profundidad,
      Altura: mueble.Altura,
    };
    return this.http.put<unknown>(`${this.apiUrl}/${mueble.Id}`, enviar);
  }

  eliminarMueble(id: number) {
    return this.http.delete<unknown>(`${this.apiUrl}/${id}`);
  }
}
