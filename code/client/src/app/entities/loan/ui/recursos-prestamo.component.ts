import { Component, Input, signal } from '@angular/core';
import { PrestamoDto } from '@entities/admin';
import { VercontratoComponent } from './contrato/vercontrato.component';
import { PrestamosAPIService } from '../api/prestamos-api.service';
import { ToastService } from '@shared/ui';

@Component({
  selector: 'app-recursos-prestamo',
  imports: [VercontratoComponent],
  template: `
    @if (tieneContrato) {
      <button
        type="button"
        class="btn btn-sm btn-ghost"
        (click)="contratoVisible.set(true)"
      >
        <i class="fas fa-file-signature" aria-hidden="true"></i> Ver contrato
      </button>
    }
    @if (puedeVerUbicacion) {
      <button
        type="button"
        class="btn btn-sm btn-ghost"
        (click)="ubicacion.showModal()"
      >
        <i class="fas fa-location-dot" aria-hidden="true"></i> Ver ubicación
      </button>
    }
    @if (prestamos.length) {
      <button
        type="button"
        class="btn btn-sm btn-ghost"
        [disabled]="actualizandoGuardado()"
        (click)="alternarGuardado()"
      >
        <i
          [class]="estaGuardado ? 'fas fa-bookmark' : 'far fa-bookmark'"
          aria-hidden="true"
        ></i>
        {{ estaGuardado ? 'Quitar de guardados' : 'Guardar para repetir' }}
      </button>
    }
    @if (contratoVisible() && prestamos.length) {
      <app-vercontrato
        [vercontraro]="contratoVisible"
        [idprestamo]="prestamos[0].Id"
      />
    }
    <dialog #ubicacion aria-label="Ubicación de los equipos">
      <header>
        <h2>Ubicación de los equipos</h2>
        <button
          type="button"
          class="modal-close-btn"
          aria-label="Cerrar ubicación"
          (click)="ubicacion.close()"
        >
          <i class="fas fa-times"></i>
        </button>
      </header>
      <div class="locations">
        @for (equipo of prestamos; track $index) {
          <section>
            <h3>
              {{ equipo.NombreGrupoEquipo }}
              @if (equipo.CodigoImt) {
                <span>IMT {{ equipo.CodigoImt }}</span>
              }
              @if (equipo.CodigoUcb) {
                <span>UCB {{ equipo.CodigoUcb }}</span>
              }
              @if (equipo.NumeroSerial) {
                <span>Serie {{ equipo.NumeroSerial }}</span>
              }
            </h3>
            <dl>
              <div>
                <dt>Ambiente</dt>
                <dd>
                  {{ equipo.UbicacionEquipo || 'Sin ambiente asignado' }}
                </dd>
              </div>
              <div>
                <dt>Mueble</dt>
                <dd>{{ equipo.NombreMueble || 'Sin mueble asignado' }}</dd>
              </div>
              <div>
                <dt>Gavetero</dt>
                <dd>{{ equipo.NombreGavetero || 'Sin gavetero asignado' }}</dd>
              </div>
              <div>
                <dt>Administrador de laboratorio</dt>
                <dd>
                  {{
                    equipo.AdministradorAmbiente || 'Sin responsable asignado'
                  }}
                </dd>
              </div>
              @if (equipo.UbicacionMueble) {
                <div>
                  <dt>Referencia</dt>
                  <dd>{{ equipo.UbicacionMueble }}</dd>
                </div>
              }
            </dl>
          </section>
        }
      </div>
    </dialog>
  `,
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }
    .btn i {
      color: var(--ink-muted);
    }
    dialog {
      width: min(620px, calc(100vw - 24px));
      max-height: calc(100dvh - 24px);
      padding: 0;
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      color: var(--ink);
      background: var(--surface);
      font-family: var(--font);
      box-sizing: border-box;
      overflow: auto;
    }
    dialog::backdrop {
      background: rgba(0, 0, 0, 0.45);
    }
    header {
      position: sticky;
      top: 0;
      background: var(--surface);
      padding: 24px 64px 20px 24px;
      border-bottom: 1px solid var(--border);
      z-index: 1;
    }
    h2 {
      font-size: 20px;
      margin: 0;
    }
    .modal-close-btn {
      position: absolute;
      right: 18px;
      top: 18px;
    }
    .locations {
      padding: 0 24px 12px;
    }
    section {
      padding: 18px 0;
      border-bottom: 1px solid var(--border);
    }
    section:last-child {
      border: 0;
    }
    h3 {
      font-size: 16px;
      margin: 0 0 16px;
      overflow-wrap: anywhere;
    }
    h3 span {
      display: block;
      font-weight: 500;
      font-size: 13px;
      color: var(--ink-secondary);
      margin-top: 4px;
    }
    dl {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 16px;
      margin: 0;
    }
    dt {
      font-size: 13px;
      color: var(--ink-secondary);
    }
    dd {
      margin: 4px 0 0;
      overflow-wrap: anywhere;
    }
    @media (max-width: 480px) {
      dl {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class RecursosPrestamoComponent {
  @Input() prestamos: PrestamoDto[] = [];
  readonly contratoVisible = signal(false);
  readonly actualizandoGuardado = signal(false);

  constructor(
    private readonly prestamosApi: PrestamosAPIService,
    private readonly toast: ToastService,
  ) {}

  get estaGuardado(): boolean {
    return this.prestamos[0]?.Guardado ?? false;
  }

  alternarGuardado(): void {
    const id = this.prestamos[0]?.Id;
    if (!id || this.actualizandoGuardado()) return;

    const nuevoEstado = !this.estaGuardado;
    this.actualizandoGuardado.set(true);
    this.prestamosApi.actualizarGuardado(id, nuevoEstado).subscribe({
      next: () => {
        for (const prestamo of this.prestamos) prestamo.Guardado = nuevoEstado;
        this.toast.success(
          nuevoEstado
            ? 'Préstamo guardado. Podrás prepararlo de nuevo desde tu historial.'
            : 'Préstamo quitado de guardados.',
        );
        this.actualizandoGuardado.set(false);
      },
      error: () => {
        this.actualizandoGuardado.set(false);
      },
    });
  }
  get tieneContrato(): boolean {
    return this.prestamos.some((p) => Number(p.IdContrato) > 0);
  }
  get puedeVerUbicacion(): boolean {
    return ['aprobado', 'activo'].includes(
      this.prestamos[0]?.EstadoPrestamo?.toLowerCase() ?? '',
    );
  }
}
