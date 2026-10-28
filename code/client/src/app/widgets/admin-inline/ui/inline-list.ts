import { Directive, OnInit } from '@angular/core';
import { Observable } from 'rxjs';

@Directive()
export abstract class InlineListComponent<T> implements OnInit {
  items: T[] = [];
  cargando = true;
  filtroTexto = '';

  get itemsFiltrados(): T[] {
    const texto = this.filtroTexto.trim().toLowerCase();
    if (!texto) return this.items;
    return this.items.filter((item) =>
      this.camposBusqueda(item).some((campo) =>
        String(campo ?? '')
          .toLowerCase()
          .includes(texto),
      ),
    );
  }

  ngOnInit(): void {
    this.cargar().subscribe({
      next: (items) => {
        this.items = items;
        this.cargando = false;
      },
      error: () => {
        this.cargando = false;
      },
    });
  }

  protected abstract cargar(): Observable<T[]>;

  protected abstract camposBusqueda(item: T): unknown[];
}
