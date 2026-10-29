import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class FiltrosService {
  categoriasSeleccionadas: Set<string> = new Set();
  solicitud: string = '';

  limpiar() {
    this.categoriasSeleccionadas.clear();
    this.solicitud = '';
  }
}
