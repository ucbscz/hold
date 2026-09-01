import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { PrestamoDto } from '@entities/admin';
import { GrupoequipoService } from '@entities/equipment-group';
import {
  PrestamosAPIService,
  RecursosPrestamoComponent,
  VistaPrestamosComponent,
} from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { CarritoService } from '@features/cart';
import { TablePaginationComponent } from '@shared/lib/admin-table';
import { MostrarerrorComponent } from '@shared/ui';
import { forkJoin } from 'rxjs';
import { HistorialBase } from '../base/historial-base';

@Component({
  selector: 'app-guardados',
  standalone: true,
  imports: [
    CommonModule,
    RecursosPrestamoComponent,
    VistaPrestamosComponent,
    MostrarerrorComponent,
    TablePaginationComponent,
  ],
  templateUrl: './guardados.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class GuardadosComponent extends HistorialBase {
  protected override estado = '';
  readonly preparando = new Set<number>();

  constructor(
    protected override usuario: UsuarioService,
    protected override prestamoApi: PrestamosAPIService,
    private readonly gruposApi: GrupoequipoService,
    private readonly carrito: CarritoService,
    private readonly router: Router,
  ) {
    super(prestamoApi, usuario);
  }

  ngOnInit(): void {
    this.cargarDatos();
  }

  override cargarDatos(): void {
    if (this.usuario.estaVacio()) return;

    this.prestamoApi
      .obtenerPrestamosPorUsuario(
        this.usuario.obtenerUsuario().id!,
        this.estado,
        true,
      )
      .subscribe({
        next: (data) => this.agruparPrestamos(data),
        error: () => {
          this.mensajeerror = 'No se pudieron cargar los préstamos guardados.';
          this.error.set(true);
        },
      });
  }

  override agruparPrestamos(datos: PrestamoDto[]): void {
    super.agruparPrestamos(datos);
  }

  prepararDeNuevo(idPrestamo: number, equipos: PrestamoDto[]): void {
    if (this.preparando.has(idPrestamo)) return;

    const cantidades = new Map<number, number>();
    for (const equipo of equipos) {
      const idGrupo = equipo.GrupoEquipoId[0];
      if (!idGrupo) continue;
      cantidades.set(idGrupo, (cantidades.get(idGrupo) ?? 0) + 1);
    }

    if (cantidades.size === 0) {
      this.mensajeerror = 'Este préstamo no conserva grupos de equipos que puedan volver a solicitarse.';
      this.error.set(true);
      return;
    }

    this.preparando.add(idPrestamo);
    forkJoin(
      Array.from(cantidades.keys()).map((id) =>
        this.gruposApi.getproducto(String(id)),
      ),
    ).subscribe({
      next: (grupos) => {
        this.carrito.vaciarCarrito();
        for (const grupo of grupos) {
          const cantidad = cantidades.get(grupo.id) ?? 1;
          this.carrito.establecerCantidad(
            grupo.id,
            grupo.nombre,
            grupo.link ?? '',
            grupo.marca ?? '',
            grupo.modelo ?? '',
            grupo.CostoPromedio ?? 0,
            grupo.Cantidad ?? cantidad,
            cantidad,
            grupo.TiempoMaximoPrestamoDias,
          );
        }
        this.preparando.delete(idPrestamo);
        void this.router.navigate(['/carrito'], { queryParams: { step: 1 } });
      },
      error: () => {
        this.preparando.delete(idPrestamo);
        this.mensajeerror = 'No se pudieron preparar los equipos. Intenta nuevamente.';
        this.error.set(true);
      },
    });
  }
}
