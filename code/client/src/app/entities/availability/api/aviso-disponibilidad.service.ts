import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AvisoDisponibilidadService {
  private readonly url = environment.apiUrl + '/api/aviso-disponibilidad';

  constructor(private readonly http: HttpClient) {}

  registrar(idGrupoEquipo: number, fecha: Date): Observable<unknown> {
    return this.http.post<unknown>(this.url, {
      IdGrupoEquipo: idGrupoEquipo,
      Fecha: fecha.toISOString(),
      Cantidad: 1,
    });
  }
}
