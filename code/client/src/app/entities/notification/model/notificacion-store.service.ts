import { computed, Injectable, signal, WritableSignal } from '@angular/core';
import { AuthService } from '@features/auth-session';
import { finalize } from 'rxjs';
import { NotificacionApiService } from '../api/notificacion.service';
import { Notificacion } from './notificacion.model';

const INTERVALO_MS = 30000;

@Injectable({
  providedIn: 'root',
})
export class NotificacionStoreService {
  private readonly _notificaciones: WritableSignal<Notificacion[]> = signal([]);
  readonly notificaciones = this._notificaciones.asReadonly();
  readonly notificacionesUsuario = computed(() =>
    this._notificaciones().filter((n) => !this.esNotificacionAdmin(n)),
  );
  readonly notificacionesAdmin = computed(() =>
    this._notificaciones().filter((n) => this.esNotificacionAdmin(n)),
  );
  readonly noLeidas = computed(
    () => this._notificaciones().filter((n) => !n.Leido).length,
  );
  readonly noLeidasUsuario = computed(
    () => this.notificacionesUsuario().filter((n) => !n.Leido).length,
  );
  readonly noLeidasAdmin = computed(
    () => this.notificacionesAdmin().filter((n) => !n.Leido).length,
  );
  private intervalo: ReturnType<typeof setInterval> | null = null;
  private actualizando = false;

  constructor(
    private readonly api: NotificacionApiService,
    private readonly auth: AuthService,
  ) {}

  iniciarPolling(): void {
    if (this.intervalo !== null) return;

    this.refrescar();
    this.intervalo = setInterval(() => this.refrescar(), INTERVALO_MS);
  }

  detenerPolling(): void {
    if (this.intervalo === null) return;

    clearInterval(this.intervalo);
    this.intervalo = null;
  }

  refrescar(): void {
    if (
      this.actualizando ||
      (typeof document !== 'undefined' && document.hidden)
    )
      return;

    if (!this.auth.isLoggedIn()) {
      this._notificaciones.set([]);
      return;
    }

    this.actualizando = true;
    this.api
      .obtenerNotificaciones()
      .pipe(finalize(() => (this.actualizando = false)))
      .subscribe({
        next: (lista) => this._notificaciones.set(lista),
        error: () => {},
      });
  }

  marcarLeida(id: number): void {
    this.api.marcarLeida(id).subscribe({
      next: () =>
        this._notificaciones.update((lista) =>
          lista.map((n) => (n.Id === id ? { ...n, Leido: true } : n)),
        ),
      error: () => {},
    });
  }

  marcarTodasLeidas(): void {
    this.api.marcarTodasLeidas().subscribe({
      next: () =>
        this._notificaciones.update((lista) =>
          lista.map((n) => ({ ...n, Leido: true })),
        ),
      error: () => {},
    });
  }

  private esNotificacionAdmin(notificacion: Notificacion): boolean {
    return notificacion.Tipo?.startsWith('Admin') ?? false;
  }
}
