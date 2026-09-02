import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { finalize, map, Observable, of, shareReplay, tap } from 'rxjs';
import {
  ComentarioEquipo,
  ComentarioEquipoOrden,
} from '../model/comentario-equipo';
import { GrupoEquipo } from '../model/grupo-equipo';
import { ComentarioEquipoApiItem } from './comentario-equipo-api-item';
import { GrupoEquipoApiItem } from './grupo-equipo-api-item';
export interface ComponenteGrupo {
  Id: number;
  Nombre: string;
  Modelo: string;
  Descripcion?: string;
  Tipo?: string;
  CodigoImtEquipo?: string;
  PrecioReferencia?: number;
}
export interface GrupoEquipoImportacion {
  Nombre: string;
  Modelo: string;
  Marca: string;
  Descripcion: string;
  UrlImagen?: string | null;
  UrlDataSheet?: string | null;
  UrlOrigen: string;
}
@Injectable({
  providedIn: 'root',
})
export class GrupoequipoService {
  private readonly apiUrl = environment.apiUrl + '/api/grupos';
  private readonly grupoEquipoApiVacio: GrupoEquipoApiItem = {
    Id: 0,
    Nombre: null,
  };
  private readonly comentarioEquipoApiVacio: ComentarioEquipoApiItem = {
    Id: 0,
    IdGrupoEquipo: 0,
    IdComentarioPadre: null,
    CarnetUsuario: '',
    NombreUsuario: '',
    Contenido: '',
    FechaCreacion: '',
    Likes: 0,
    LikedByCurrentUser: false,
    PuedeEliminar: false,
    Respuestas: [],
  };
  private readonly cache = new Map<string, GrupoEquipo[]>();
  private readonly cachePorId = new Map<number, GrupoEquipo>();
  private readonly solicitudesPorId = new Map<
    number,
    Observable<GrupoEquipo>
  >();
  paginaGuardada: number = 0;
  cantidadObjetosGuardada: number = 21;
  constructor(private readonly http: HttpClient) {}

  obtenerComponentes(id: number, pagina = 1) {
    return this.http
      .get<ApiResponse<ComponenteGrupo[]>>(
        `${this.apiUrl}/${id}/componentes?pagina=${pagina}`,
      )
      .pipe(map((r) => extractApiValue(r, [])));
  }

  invalidarCache() {
    this.cache.clear();
    this.cachePorId.clear();
    this.solicitudesPorId.clear();
  }

  private mapearGrupoEquipo(item: GrupoEquipoApiItem): GrupoEquipo {
    return {
      id: item.Id,
      nombre: item.Nombre,
      descripcion: item.Descripcion || '',
      modelo: item.Modelo ? ` ${item.Modelo}` : '',
      url_data_sheet: item.UrlDataSheet || '',
      marca: item.Marca ? ` ${item.Marca}` : '',
      link: item.UrlImagen ?? null,
      nombreCategoria: item.NombreCategoria || '',
      Cantidad: item.Cantidad || 0,
      CostoPromedio: item.CostoPromedio || 0,
      TiempoMaximoPrestamoDias: item.TiempoMaximoPrestamoDias ?? 7,
    };
  }

  private mapearComentario(item: ComentarioEquipoApiItem): ComentarioEquipo {
    return {
      id: item.Id,
      idGrupoEquipo: item.IdGrupoEquipo,
      idComentarioPadre: item.IdComentarioPadre ?? null,
      carnetUsuario: item.CarnetUsuario,
      nombreUsuario: item.NombreUsuario,
      contenido: item.Contenido,
      fechaCreacion: item.FechaCreacion,
      likes: item.Likes ?? 0,
      likedByCurrentUser: item.LikedByCurrentUser ?? false,
      puedeEliminar: item.PuedeEliminar ?? false,
      respuestas: (item.Respuestas ?? []).map((respuesta) =>
        this.mapearComentario(respuesta),
      ),
    };
  }

  crearGrupoEquipo(grupoEquipo: GrupoEquipo) {
    const envio = {
      Nombre: grupoEquipo.nombre,
      Modelo: grupoEquipo.modelo,
      Marca: grupoEquipo.marca,
      NombreCategoria: grupoEquipo.nombreCategoria,
      Descripcion: grupoEquipo.descripcion,
      UrlDataSheet: grupoEquipo.url_data_sheet,
      UrlImagen: grupoEquipo.link,
      TiempoMaximoPrestamoDias: grupoEquipo.TiempoMaximoPrestamoDias,
    };
    return this.http
      .post<unknown>(this.apiUrl, envio)
      .pipe(tap(() => this.invalidarCache()));
  }

  importarDesdeUrl(url: string): Observable<GrupoEquipoImportacion> {
    return this.http
      .post<ApiResponse<GrupoEquipoImportacion>>(`${this.apiUrl}/importar`, {
        Url: url,
      })
      .pipe(
        map((response) =>
          extractApiValue(response, {
            Nombre: '',
            Modelo: '',
            Marca: '',
            Descripcion: '',
            UrlOrigen: url,
          }),
        ),
      );
  }

  obtenersinfiltroGruposEquipos(): Observable<GrupoEquipo[]> {
    return this.http.get<ApiResponse<GrupoEquipoApiItem[]>>(this.apiUrl).pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => ({
          ...this.mapearGrupoEquipo(item),
          modelo: item.Modelo,
          marca: item.Marca,
          descripcion: item.Descripcion,
          url_data_sheet: item.UrlDataSheet,
        })),
      ),
    );
  }

  getGrupoEquipo(
    categoria: string,
    producto: string,
  ): Observable<GrupoEquipo[]> {
    const categoriaNormalizada = categoria.trim();
    const productoNormalizado = producto.trim();
    const cacheKey = `${productoNormalizado}|${categoriaNormalizada}`;
    const cached = this.cache.get(cacheKey);

    if (cached) return of(cached);

    const request =
      productoNormalizado || categoriaNormalizada
        ? this.http.get<ApiResponse<GrupoEquipoApiItem[]>>(this.apiUrl, {
            params: new HttpParams()
              .set('nombre', productoNormalizado)
              .set('categoria', categoriaNormalizada),
          })
        : this.http.get<ApiResponse<GrupoEquipoApiItem[]>>(this.apiUrl);

    return request.pipe(
      map((data) =>
        extractApiValue(data, []).map((item) => this.mapearGrupoEquipo(item)),
      ),
      tap((result) => {
        this.cache.set(cacheKey, result);
        this.guardarGruposPorId(result);
      }),
    );
  }

  getproducto(id: string): Observable<GrupoEquipo> {
    const idNumerico = Number(id);
    const cached = this.cachePorId.get(idNumerico);
    if (cached) return of({ ...cached });

    const solicitudExistente = this.solicitudesPorId.get(idNumerico);
    if (solicitudExistente) return solicitudExistente;

    const url = `${this.apiUrl}/${id}`;
    const solicitud = this.http.get<ApiResponse<GrupoEquipoApiItem>>(url).pipe(
      map((data) =>
        this.mapearGrupoEquipo(extractApiValue(data, this.grupoEquipoApiVacio)),
      ),
      tap((grupo) => {
        if (grupo.id) this.cachePorId.set(grupo.id, { ...grupo });
      }),
      finalize(() => this.solicitudesPorId.delete(idNumerico)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    this.solicitudesPorId.set(idNumerico, solicitud);
    return solicitud;
  }

  obtenerComentarios(
    idGrupoEquipo: number,
    orden: ComentarioEquipoOrden = 'recientes',
  ): Observable<ComentarioEquipo[]> {
    return this.http
      .get<ApiResponse<ComentarioEquipoApiItem[]>>(
        `${this.apiUrl}/${idGrupoEquipo}/comentarios`,
        {
          params: new HttpParams().set('orden', orden),
        },
      )
      .pipe(
        map((data) =>
          extractApiValue(data, []).map((item) => this.mapearComentario(item)),
        ),
      );
  }

  crearComentario(
    idGrupoEquipo: number,
    contenido: string,
    idComentarioPadre?: number,
  ): Observable<ComentarioEquipo> {
    return this.http
      .post<ApiResponse<ComentarioEquipoApiItem>>(
        `${this.apiUrl}/${idGrupoEquipo}/comentarios`,
        {
          Contenido: contenido,
          IdComentarioPadre: idComentarioPadre ?? null,
        },
      )
      .pipe(
        map((data) =>
          this.mapearComentario(
            extractApiValue(data, this.comentarioEquipoApiVacio),
          ),
        ),
      );
  }

  alternarLikeComentario(
    idGrupoEquipo: number,
    idComentario: number,
  ): Observable<ComentarioEquipo> {
    return this.http
      .post<ApiResponse<ComentarioEquipoApiItem>>(
        `${this.apiUrl}/${idGrupoEquipo}/comentarios/${idComentario}/likes`,
        {},
      )
      .pipe(
        map((data) =>
          this.mapearComentario(
            extractApiValue(data, this.comentarioEquipoApiVacio),
          ),
        ),
      );
  }

  eliminarComentario(
    idGrupoEquipo: number,
    idComentario: number,
  ): Observable<unknown> {
    return this.http.delete(
      `${this.apiUrl}/${idGrupoEquipo}/comentarios/${idComentario}`,
    );
  }

  editarGrupoEquipo(grupoEquipo: GrupoEquipo) {
    const envio = {
      Id: grupoEquipo.id,
      Nombre: grupoEquipo.nombre,
      Modelo: grupoEquipo.modelo,
      Marca: grupoEquipo.marca,
      NombreCategoria: grupoEquipo.nombreCategoria,
      Descripcion: grupoEquipo.descripcion,
      UrlDataSheet: grupoEquipo.url_data_sheet,
      UrlImagen: grupoEquipo.link,
      TiempoMaximoPrestamoDias: grupoEquipo.TiempoMaximoPrestamoDias,
    };
    return this.http
      .put<unknown>(`${this.apiUrl}/${grupoEquipo.id}`, envio)
      .pipe(tap(() => this.invalidarCache()));
  }

  eliminarGrupoEquipo(id: number) {
    return this.http
      .delete<unknown>(`${this.apiUrl}/${id}`)
      .pipe(tap(() => this.invalidarCache()));
  }

  private guardarGruposPorId(grupos: GrupoEquipo[]): void {
    for (const grupo of grupos) {
      if (grupo.id) this.cachePorId.set(grupo.id, { ...grupo });
    }
  }
}
