import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Categorias } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { CategoriaApiItem } from './categoria-api-item';
@Injectable({
  providedIn: 'root',
})
export class CategoriaService {
  private readonly apiurl = environment.apiUrl + '/api/categorias';
  constructor(private readonly http: HttpClient) {}

  obtenercategorias(): Observable<Categorias[]> {
    return this.http.get<ApiResponse<CategoriaApiItem[]>>(this.apiurl).pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => ({
          Id: item.Id,
          Nombre: item.Nombre,
        })),
      ),
    );
  }

  crearCategoria(categoria: Categorias) {
    const envio = {
      Nombre: categoria.Nombre,
    };
    return this.http.post<unknown>(this.apiurl, envio);
  }

  actualizarCategoria(categoria: Categorias) {
    const envio = {
      Id: categoria.Id,
      Nombre: categoria.Nombre,
    };
    return this.http.put<unknown>(`${this.apiurl}/${categoria.Id}`, envio);
  }

  eliminarCategoria(id: number) {
    return this.http.delete<unknown>(this.apiurl + '/' + id);
  }
}
