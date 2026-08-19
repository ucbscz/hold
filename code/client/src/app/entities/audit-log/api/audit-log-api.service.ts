import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AuditLogDto } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { AuditLogApiItem } from './audit-log-api-item';

@Injectable({ providedIn: 'root' })
export class AuditLogApiService {
  private readonly url = `${environment.apiUrl}/api/audit-log`;

  constructor(private readonly http: HttpClient) {}

  getAuditLog(
    entidad?: string,
    carnet?: string,
    desde?: string,
    hasta?: string,
  ): Observable<AuditLogDto[]> {
    let params = new HttpParams();
    if (entidad) params = params.set('entidad', entidad);
    if (carnet) params = params.set('carnet', carnet);
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);

    return this.http
      .get<ApiResponse<AuditLogApiItem[]> | AuditLogApiItem[]>(this.url, {
        params,
      })
      .pipe(
        map((res) => {
          const data = extractApiValue(res, []);
          return data.map((item) => ({
            Id: item.Id,
            AdminCarnet: item.AdminCarnet ?? undefined,
            AdminNombre: item.AdminNombre ?? undefined,
            Accion: item.Accion ?? undefined,
            Entidad: item.Entidad ?? undefined,
            EntidadId:
              item.EntidadId === null || item.EntidadId === undefined
                ? undefined
                : String(item.EntidadId),
            Detalle: item.Detalle ?? undefined,
            Timestamp: item.Timestamp ? new Date(item.Timestamp) : undefined,
          }));
        }),
      );
  }
}
