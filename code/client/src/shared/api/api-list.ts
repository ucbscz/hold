import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { ApiResponse } from './api-response';
import { extractApiValue } from './api-response-utils';

export function getApiList<T>(
  http: HttpClient,
  url: string,
  filtros: Record<string, string | number> = {},
): Observable<T[]> {
  return http
    .get<ApiResponse<T[]>>(url, {
      params: new HttpParams({ fromObject: filtros }),
    })
    .pipe(map((respuesta) => extractApiValue(respuesta, [])));
}
