import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Mantenimientos } from '@entities/admin';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map, Observable } from 'rxjs';
import { MantenimientoCreationForm } from '../model/mantenimiento-creation-form';
import { MantenimientoApiItem } from './mantenimiento-api-item';
@Injectable({
  providedIn: 'root',
})
export class MantenimientoService {
  private readonly apiUrl = environment.apiUrl + '/api/mantenimiento';
  constructor(private readonly http: HttpClient) {}

  crearMantenimiento(
    mantenimiento: MantenimientoCreationForm,
    equipos: Map<
      number,
      { TipoMantenimiento: string; DescripcionEquipo: string; nombre: string }
    >,
  ) {
    const codigosImt: number[] = [];
    const tiposMantenimiento: string[] = [];
    const descripcionesEquipos: string[] = [];

    equipos.forEach((value, key) => {
      codigosImt.push(key);
      tiposMantenimiento.push(value.TipoMantenimiento);
      descripcionesEquipos.push(value.DescripcionEquipo);
    });

    const envio = {
      FechaMantenimiento: mantenimiento.FechaMantenimiento,
      FechaFinalMantenimiento: mantenimiento.FechaFinalDeMantenimiento,
      NombreEmpresaMantenimiento: mantenimiento.NombreEmpresaMantenimiento,
      Costo: mantenimiento.Costo,
      Descripcion: mantenimiento.DescripcionMantenimiento,
      CodigoImt: codigosImt,
      TiposMantenimiento: tiposMantenimiento,
      DescripcionesEquipo: descripcionesEquipos,
    };
    return this.http.post<unknown>(this.apiUrl, envio);
  }

  obtenerMantenimientos(): Observable<Mantenimientos[]> {
    return this.http.get<ApiResponse<MantenimientoApiItem[]>>(this.apiUrl).pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => ({
          Id: item.Id,
          IdEmpresa: null,
          NombreEmpresaMantenimiento: item.NombreEmpresaMantenimiento,
          FechaMantenimiento: item.FechaMantenimiento
            ? new Date(item.FechaMantenimiento)
            : null,
          FechaFinalDeMantenimiento: item.FechaFinalMantenimiento
            ? new Date(item.FechaFinalMantenimiento)
            : null,
          Costo: item.Costo,
          Descripcion: item.Descripcion,
          TipoMantenimiento: item.TipoMantenimiento,
          NombreGrupoEquipo: item.NombreGrupoEquipo,
          CodigoImtEquipo: item.CodigoImtEquipo,
          DescripcionEquipo: item.DescripcionEquipo,
        })),
      ),
    );
  }

  eliminarMantenimiento(id: number) {
    return this.http.delete<unknown>(`${this.apiUrl}/${id}`);
  }

  actualizarMantenimiento(
    mantenimientos: Mantenimientos[],
  ): Observable<unknown> {
    const principal = mantenimientos[0];
    if (!principal?.Id) throw new Error('Mantenimiento inválido');

    return this.http.put<unknown>(`${this.apiUrl}/${principal.Id}`, {
      NombreEmpresaMantenimiento: principal.NombreEmpresaMantenimiento,
      FechaMantenimiento: principal.FechaMantenimiento,
      FechaFinalMantenimiento: principal.FechaFinalDeMantenimiento,
      Costo: principal.Costo,
      Descripcion: principal.Descripcion,
      CodigoImt: mantenimientos
        .map((mantenimiento) => Number(mantenimiento.CodigoImtEquipo))
        .filter(Number.isFinite),
      TiposMantenimiento: mantenimientos.map(
        (mantenimiento) => mantenimiento.TipoMantenimiento ?? '',
      ),
      DescripcionesEquipo: mantenimientos.map(
        (mantenimiento) => mantenimiento.DescripcionEquipo ?? '',
      ),
    });
  }
}
