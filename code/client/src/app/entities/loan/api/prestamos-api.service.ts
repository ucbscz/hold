import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { PrestamoDto } from '@entities/admin';
import { Carrito } from '@entities/cart';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map } from 'rxjs';
import { PrestamoApiItem } from './prestamo-api-item';
@Injectable({
  providedIn: 'root',
})
export class PrestamosAPIService {
  private readonly url = environment.apiUrl + '/api/Prestamo';
  constructor(private readonly http: HttpClient) {}

  private mapearPrestamo(item: PrestamoApiItem): PrestamoDto {
    return {
      Id: item.Id,
      CarnetUsuario: item.CarnetUsuario,
      NombreUsuario: item.NombreUsuario,
      ApellidoPaternoUsuario: item.ApellidoPaternoUsuario,
      TelefonoUsuario: item.TelefonoUsuario,
      NombreGrupoEquipo: item.NombreGrupoEquipo,
      CodigoImt: item.CodigoImt,
      FechaSolicitud: item.FechaSolicitud
        ? new Date(item.FechaSolicitud)
        : null,
      FechaPrestamoEsperada: item.FechaPrestamoEsperada
        ? new Date(item.FechaPrestamoEsperada)
        : null,
      FechaPrestamo: item.FechaPrestamo ? new Date(item.FechaPrestamo) : null,
      FechaDevolucionEsperada: item.FechaDevolucionEsperada
        ? new Date(item.FechaDevolucionEsperada)
        : null,
      FechaDevolucion: item.FechaDevolucion
        ? new Date(item.FechaDevolucion)
        : null,
      Observacion: item.Observacion,
      EstadoPrestamo: item.EstadoPrestamo,
      IdContrato: item.IdContrato,
      Ubicacion_Equipo: item.UbicacionEquipo || item.Ubicacion_Equipo || null,
      Nombre_Gavetero: item.NombreGavetero || item.Nombre_Gavetero || null,
      Nombre_Mueble: item.NombreMueble || item.Nombre_Mueble || null,
      Ubicacion_Mueble: item.UbicacionMueble || item.Ubicacion_Mueble || null,
    };
  }

  obtenerPrestamos() {
    return this.http
      .get<ApiResponse<PrestamoApiItem[]> | PrestamoApiItem[]>(this.url)
      .pipe(
        map((data) => {
          const arrayData = Array.isArray(data)
            ? data
            : extractApiValue(data, []);
          return arrayData.map((item) => this.mapearPrestamo(item));
        }),
      );
  }

  crearPrestamo(carrito: Carrito, carnet: string, contrato: string | null) {
    const grupoid: number[] = [];
    for (const [key, item] of Object.entries(carrito)) {
      if (item.cantidad > 0) {
        for (let i = 0; i < item.cantidad; i++) {
          grupoid.push(Number(key));
        }
      }
    }
    const fechas = Object.values(carrito)[0];
    const body = {
      GrupoEquipoId: grupoid,
      FechaPrestamoEsperada: fechas?.fecha_inicio || null,
      FechaDevolucionEsperada: fechas?.fecha_final || null,
      CarnetUsuario: carnet,
      Observacion: '',
      Contrato: contrato || null,
    };

    return this.http.post(this.url, body);
  }

  eliminarPrestamo(id: number) {
    return this.http.delete(`${this.url}/${id}`);
  }

  estadoReserva() {
    return this.http
      .get<ApiResponse<{ PuedeReservar: boolean; Motivo: string | null }>>(
        `${this.url}/estado-reserva`,
      )
      .pipe(
        map((data) => {
          const valor = extractApiValue(data, {
            PuedeReservar: true,
            Motivo: null,
          });
          return {
            puedeReservar: valor.PuedeReservar,
            motivo: valor.Motivo,
          };
        }),
      );
  }

  cambiarEstadoPrestamo(
    Id: number,
    estado: string,
    observacion?: string,
    equiposRetorno?: { CodigoImt: string; EstadoEquipo: string }[],
  ) {
    let params = `estado=${encodeURIComponent(estado)}`;
    if (
      observacion !== undefined &&
      observacion !== null &&
      observacion !== ''
    ) {
      params += `&observacion=${encodeURIComponent(observacion)}`;
    }
    const body =
      equiposRetorno && equiposRetorno.length > 0
        ? { EquiposRetorno: equiposRetorno }
        : null;
    return this.http.put(`${this.url}/${Id}/estado?${params}`, body);
  }

  obtenerPrestamosPorUsuario(carnet: string, estadoPrestamo: string) {
    const APIurl = `${this.url}/historial?carnetUsuario=${carnet}&estadoPrestamo=${estadoPrestamo}`;
    return this.http
      .get<ApiResponse<PrestamoApiItem[]> | PrestamoApiItem[]>(APIurl)
      .pipe(
        map((data) => {
          const arrayData = Array.isArray(data)
            ? data
            : extractApiValue(data, []);
          if (!Array.isArray(arrayData)) {
            return [];
          }
          return arrayData.map((item) => this.mapearPrestamo(item));
        }),
      );
  }

  obtenercontratoPrestamo(id: number) {
    const APIurl = `${this.url}/contrato/${id}`;
    return this.http.get<{ contrato: string }>(APIurl).pipe(
      map((response) => {
        if (!response || !response.contrato) return '';
        return response.contrato;
      }),
    );
  }
}
