import { Injectable } from '@angular/core';
import { Carrito } from '@entities/cart';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class CarritoService {
  private carrito: Carrito = {};
  private cantidadTotal = 0;
  private readonly carritoSubject = new BehaviorSubject<Carrito>({});
  readonly carrito$ = this.carritoSubject.asObservable();

  agregarProducto(
    id: number,
    nombre: string | null,
    imagen: string,
    marca: string,
    modelo: string,
    precio: number,
    cantidadMaxima: number,
  ): void {
    if (!nombre) {
      return;
    }

    if (!this.carrito[id]) {
      this.carrito[id] = {
        nombre,
        modelo,
        marca,
        cantidad: 1,
        fecha_inicio: null,
        fecha_final: null,
        imagen,
        precio,
        cantidadMax: cantidadMaxima,
      };
      this.cantidadTotal++;
    } else if (this.carrito[id].cantidad < this.carrito[id].cantidadMax) {
      this.carrito[id].cantidad += 1;
      this.cantidadTotal++;
    }

    this.emitirCambios();
  }

  quitarProducto(id: number): void {
    if (id in this.carrito) {
      if (this.carrito[id].cantidad > 1) {
        this.carrito[id].cantidad -= 1;
        this.cantidadTotal--;
      } else {
        delete this.carrito[id];
        this.cantidadTotal--;
      }
    }

    this.emitirCambios();
  }

  eliminarProducto(id: number): void {
    const item = this.carrito[id];

    if (!item) return;

    this.cantidadTotal -= item.cantidad;
    delete this.carrito[id];
    this.emitirCambios();
  }

  contieneProducto(id: number): boolean {
    return !!this.carrito[id];
  }

  obtenerCarrito(): Carrito {
    return this.carrito;
  }

  obtenerTotal(): number {
    return this.cantidadTotal;
  }

  vaciarCarrito(): void {
    this.carrito = {};
    this.cantidadTotal = 0;
    this.emitirCambios();
  }

  calcularPrecioTotal(): number {
    let total = 0;
    for (const clave in this.carrito) {
      total += this.carrito[clave].precio * this.carrito[clave].cantidad;
    }
    return total;
  }

  editarCantidad(id: number, cantidad: number): void {
    const item = this.carrito[id];
    if (!item) return;

    const cantidadNormalizada = Math.max(
      0,
      Math.min(Math.floor(Number(cantidad) || 0), item.cantidadMax),
    );
    this.cantidadTotal += cantidadNormalizada - item.cantidad;

    if (cantidadNormalizada === 0) delete this.carrito[id];
    else item.cantidad = cantidadNormalizada;

    this.emitirCambios();
  }

  establecerCantidad(
    id: number,
    nombre: string | null,
    imagen: string,
    marca: string,
    modelo: string,
    precio: number,
    cantidadMaxima: number,
    cantidad: number,
  ): void {
    if (!nombre) return;

    if (!this.carrito[id]) {
      this.carrito[id] = {
        nombre,
        modelo,
        marca,
        cantidad: 1,
        fecha_inicio: null,
        fecha_final: null,
        imagen,
        precio,
        cantidadMax: cantidadMaxima,
      };
    }

    this.editarCantidad(id, cantidad);
  }

  actualizarFechas(fechaInicio: string, fechaFinal: string): void {
    Object.values(this.carrito).forEach((item) => {
      item.fecha_inicio = fechaInicio;
      item.fecha_final = fechaFinal;
    });
    this.emitirCambios();
  }

  obtenerFechaInicio(): string | null {
    const items = Object.values(this.carrito);
    return items.length > 0 ? items[0].fecha_inicio : null;
  }

  obtenerFechaFinal(): string | null {
    const items = Object.values(this.carrito);
    return items.length > 0 ? items[0].fecha_final : null;
  }

  private emitirCambios(): void {
    this.carrito = { ...this.carrito };
    this.carritoSubject.next(this.carrito);
  }
}
