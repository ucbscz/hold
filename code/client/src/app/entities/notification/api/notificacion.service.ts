import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { Notificacion } from '../model/notificacion.model';

@Injectable({
  providedIn: 'root',
})
export class NotificacionApiService {
  private readonly url = environment.apiUrl + '/api/notificacion';

  constructor(private readonly http: HttpClient) {}

  obtenerNotificaciones(): Observable<Notificacion[]> {
    return this.http
      .get<ApiResponse<Notificacion[]>>(this.url)
      .pipe(map((data) => extractApiValue(data, [])));
  }

  marcarLeida(id: number): Observable<unknown> {
    return this.http.put<unknown>(`${this.url}/${id}/leido`, {});
  }

  marcarTodasLeidas(): Observable<unknown> {
    return this.http.put<unknown>(`${this.url}/leidos`, {});
  }
}
