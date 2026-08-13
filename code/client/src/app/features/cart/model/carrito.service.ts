import { Injectable } from '@angular/core';
import { Carrito } from '@entities/cart';

@Injectable({
  providedIn: 'root',
})
export class CarritoService {
  private carrito: Carrito = {};
  private cantidadTotal = 0;

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
  }

  eliminarProducto(id: number): void {
    const item = this.carrito[id];

    if (!item) return;

    this.cantidadTotal -= item.cantidad;
    delete this.carrito[id];
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
  }

  calcularPrecioTotal(): number {
    let total = 0;
    for (const clave in this.carrito) {
      total += this.carrito[clave].precio * this.carrito[clave].cantidad;
    }
    return total;
  }

  editarCantidad(id: number, cantidad: number): void {
    if (!this.carrito[id]) {
      return;
    }

    if (this.carrito[id].cantidad < cantidad) {
      this.cantidadTotal += cantidad - this.carrito[id].cantidad;
      this.carrito[id].cantidad = cantidad;
    } else if (this.carrito[id].cantidad > cantidad) {
      this.cantidadTotal -= this.carrito[id].cantidad - cantidad;
      this.carrito[id].cantidad = cantidad;
    }

    if (this.carrito[id].cantidad <= 0) {
      delete this.carrito[id];
    }
  }

  obtenerFechaInicio(): string | null {
    const items = Object.values(this.carrito);
    return items.length > 0 ? items[0].fecha_inicio : null;
  }

  obtenerFechaFinal(): string | null {
    const items = Object.values(this.carrito);
    return items.length > 0 ? items[0].fecha_final : null;
  }
}
