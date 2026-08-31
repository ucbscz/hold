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
  private readonly url = environment.apiUrl + '/api/prestamos';
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
      MotivoRechazo: item.MotivoRechazo,
      AutorizadoPor: item.AutorizadoPor,
      EntregadoPor: item.EntregadoPor,
      EstadoPrestamo: item.EstadoPrestamo,
      IdContrato: item.IdContrato,
      Ubicacion_Equipo: item.UbicacionEquipo ?? null,
      Nombre_Gavetero: item.NombreGavetero ?? null,
      Nombre_Mueble: item.NombreMueble ?? null,
      Ubicacion_Mueble: item.UbicacionMueble ?? null,
      Administrador_Ambiente: item.AdministradorAmbiente ?? null,
      TipoUsuario: item.TipoUsuario ?? null,
      DestinoPrestamo: item.DestinoPrestamo,
      IdCarrera: item.IdCarrera ?? null,
      NombreMateria: item.NombreMateria ?? null,
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

  crearPrestamo(
    carrito: Carrito,
    carnet: string,
    contrato?: string,
    destinoPrestamo: string = 'Universidad',
    idCarrera?: number,
    nombreMateria?: string,
  ) {
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
      DestinoPrestamo: destinoPrestamo,
      IdCarrera: idCarrera ?? null,
      NombreMateria: nombreMateria ?? null,
    };

    return this.http.post(this.url, body);
  }

  eliminarPrestamo(id: number) {
    return this.http.delete(`${this.url}/${id}`);
  }

  estadoReserva() {
    return this.http
      .get<ApiResponse<{ PuedeReservar: boolean; Motivo: string | null }>>(
        `${this.url}/elegibilidad`,
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
    const body = {
      EstadoPrestamo: estado,
      Observacion: observacion || null,
      EquiposRetorno: equiposRetorno ?? [],
    };
    return this.http.patch(`${this.url}/${Id}/estado`, body);
  }

  editarObservacion(id: number, observacion: string) {
    return this.http.patch(`${this.url}/${id}/observacion`, {
      Observacion: observacion,
    });
  }

  obtenerPrestamosPorUsuario(carnet: string, estadoPrestamo: string) {
    const APIurl = `${this.url}?carnet=${encodeURIComponent(carnet)}&estado=${encodeURIComponent(estadoPrestamo)}`;
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
    const APIurl = `${environment.apiUrl}/api/contratos/${id}`;
    return this.http
      .get<ApiResponse<{ ContratoHtml?: string }>>(APIurl)
      .pipe(
        map((response) => extractApiValue(response, {}).ContratoHtml ?? ''),
      );
  }
}
