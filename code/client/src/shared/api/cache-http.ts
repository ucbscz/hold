import { HttpContextToken, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

export const CONSULTA_SIN_CAMBIOS = new HttpContextToken<boolean>(() => false);

interface RespuestaCacheada {
  expira: number;
  respuesta: HttpResponse<unknown>;
}

@Injectable({ providedIn: 'root' })
export class CacheHttpStore {
  private readonly ttlMs = 60_000;
  private readonly respuestas = new Map<string, RespuestaCacheada>();
  private versionActual = 0;

  get version(): number {
    return this.versionActual;
  }

  obtener(clave: string): HttpResponse<unknown> | null {
    const entrada = this.respuestas.get(clave);

    if (!entrada) return null;
    if (entrada.expira > Date.now()) return entrada.respuesta.clone();

    this.respuestas.delete(clave);
    return null;
  }

  guardar(
    clave: string,
    respuesta: HttpResponse<unknown>,
    version: number,
  ): void {
    if (version !== this.versionActual) return;

    this.respuestas.set(clave, {
      expira: Date.now() + this.ttlMs,
      respuesta: respuesta.clone(),
    });
  }

  eliminar(clave: string): void {
    this.respuestas.delete(clave);
  }

  invalidar(): void {
    this.versionActual++;
    this.respuestas.clear();
  }
}
