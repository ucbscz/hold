import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { EmpresaMantenimiento } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { EmpresaMantenimientoApiItem } from './empresa-mantenimiento-api-item';
@Injectable({
  providedIn: 'root',
})
export class EmpresamantenimientoService {
  private readonly apiUrl = environment.apiUrl + '/api/empresas';
  constructor(private readonly http: HttpClient) {}

  crearEmpresaMantenimiento(empresa: EmpresaMantenimiento) {
    const envio = {
      NombreEmpresa: empresa.NombreEmpresa,
      NombreResponsable: empresa.NombreResponsable,
      ApellidoResponsable: empresa.ApellidoResponsable,
      Telefono: empresa.Telefono,
      Nit: empresa.Nit,
      Direccion: empresa.Direccion,
    };
    return this.http.post<unknown>(this.apiUrl, envio);
  }

  obtenerEmpresaMantenimiento(): Observable<EmpresaMantenimiento[]> {
    return this.http
      .get<ApiResponse<EmpresaMantenimientoApiItem[]>>(this.apiUrl)
      .pipe(
        map((data) =>
          extractApiValue(data, []).map((item) => ({
            Id: item.Id,
            NombreEmpresa: item.NombreEmpresa,
            NombreResponsable: item.NombreResponsable,
            ApellidoResponsable: item.ApellidoResponsable,
            Telefono: item.Telefono,
            Direccion: item.Direccion,
          })),
        ),
      );
  }

  actualizarEmpresaMantenimiento(empresa: EmpresaMantenimiento) {
    const envio = {
      Id: empresa.Id,
      NombreEmpresa: empresa.NombreEmpresa,
      NombreResponsable: empresa.NombreResponsable,
      ApellidoResponsable: empresa.ApellidoResponsable,
      Telefono: empresa.Telefono,
      Nit: empresa.Nit,
      Direccion: empresa.Direccion,
    };
    return this.http.put<unknown>(`${this.apiUrl}/${empresa.Id}`, envio);
  }

  eliminarEmpresaMantenimiento(id: number) {
    return this.http.delete<unknown>(`${this.apiUrl}/${id}`);
  }
}
