import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map } from 'rxjs';

export interface CatalogoInventario {
  Id: number;
  Nombre: string;
}
export type TipoCatalogo = 'ambientes' | 'procedencias';

@Injectable({ providedIn: 'root' })
export class CatalogoInventarioService {
  constructor(private readonly http: HttpClient) {}
  listar(tipo: TipoCatalogo) {
    return this.http
      .get<ApiResponse<CatalogoInventario[]>>(
        `${environment.apiUrl}/api/${tipo}`,
      )
      .pipe(
        map((r) =>
          extractApiValue(r, []).sort((a, b) =>
            a.Nombre.localeCompare(b.Nombre, 'es'),
          ),
        ),
      );
  }
  guardar(tipo: TipoCatalogo, nombre: string, id?: number) {
    const url = `${environment.apiUrl}/api/${tipo}`;
    return id
      ? this.http.put(`${url}/${id}`, { Nombre: nombre.trim() })
      : this.http.post(url, { Nombre: nombre.trim() });
  }
  eliminar(tipo: TipoCatalogo, id: number) {
    return this.http.delete(`${environment.apiUrl}/api/${tipo}/${id}`);
  }
}
