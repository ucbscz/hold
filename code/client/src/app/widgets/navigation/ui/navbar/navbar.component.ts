import { CommonModule, DatePipe, Location } from '@angular/common';
import {
  Component,
  effect,
  HostListener,
  signal,
  WritableSignal,
} from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { Notificacion, NotificacionStoreService } from '@entities/notification';
import { UsuarioService } from '@entities/user';
import { CarritoService } from '@features/cart';
import { parseJsonResult } from '@shared/lib/result';
import { SidebarService } from '@widgets/admin-sidebar';
import { filter, Observable } from 'rxjs';
import { UsuarioPrevioComponent } from './usuario-previo/usuario-previo.component';
@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule, DatePipe, UsuarioPrevioComponent],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'],
})
export class NavbarComponent {
  showUserMenu: WritableSignal<boolean> = signal(false);
  showNotifications: WritableSignal<boolean> = signal(false);
  activeNotificationTab: WritableSignal<'usuario' | 'admin'> =
    signal('usuario');
  expandedNotificationId: WritableSignal<number | null> = signal(null);
  showAdminSidebarToggle: WritableSignal<boolean> = signal(false);
  showHome: WritableSignal<boolean> = signal(true);
  showBack: WritableSignal<boolean> = signal(true);
  showCart: WritableSignal<boolean> = signal(true);
  showProfile: WritableSignal<boolean> = signal(true);
  readonly totalProductos$: Observable<number>;

  constructor(
    private readonly carrito: CarritoService,
    private readonly router: Router,
    private readonly usuario: UsuarioService,
    private readonly location: Location,
    private readonly sidebarService: SidebarService,
    readonly notifStore: NotificacionStoreService,
  ) {
    this.totalProductos$ = this.carrito.total$;
    this.updateButtonVisibility(this.router.url);
    this.notifStore.iniciarPolling();

    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.updateButtonVisibility(event.urlAfterRedirects || event.url);
        this.notifStore.refrescar();
      });

    effect(() => {
      this.usuario.obtenerRol();

      this.updateButtonVisibility(this.router.url);
    });
  }

  private updateButtonVisibility(url: string) {
    const cleanUrl = url.split('?')[0];
    const rol = this.usuario.obtenerRol();
    const isLoginOrRegister =
      cleanUrl.includes('/login') || cleanUrl.includes('/sign-up');

    if (isLoginOrRegister) {
      this.showHome.set(false);
      this.showBack.set(false);
      this.showCart.set(false);
      this.showProfile.set(false);
      this.showAdminSidebarToggle.set(false);
      return;
    }

    const isAdmin = rol === 'administrador';
    const isInAdminMode = isAdmin && cleanUrl.includes('/admin');

    this.showProfile.set(true);

    this.showCart.set(!isInAdminMode);

    if (isInAdminMode) {
      this.showBack.set(false);
      this.showHome.set(false);
      this.showAdminSidebarToggle.set(true);
    } else {
      this.showAdminSidebarToggle.set(false);

      if (cleanUrl.includes('/home')) {
        this.showHome.set(false);
        this.showBack.set(false);
      } else if (
        cleanUrl.includes('/cart') ||
        cleanUrl.includes('/equipment') ||
        cleanUrl.includes('/reservation')
      ) {
        this.showHome.set(false);
        this.showBack.set(true);
      } else if (
        cleanUrl.includes('/profile') ||
        cleanUrl.includes('/loan-history')
      ) {
        this.showHome.set(true);
        this.showBack.set(false);
      } else {
        this.showHome.set(true);
        this.showBack.set(true);
      }
    }
  }

  botonhome() {
    if (this.usuario.estaVacio()) {
      this.router.navigate(['/login']);
    } else {
      this.router.navigate(['/home']);
    }
  }

  toggleUserMenu() {
    this.showNotifications.set(false);
    this.showUserMenu.set(!this.showUserMenu());
  }

  toggleNotifications() {
    this.showUserMenu.set(false);
    this.sidebarService.close();
    const next = !this.showNotifications();
    this.showNotifications.set(next);

    if (next) this.notifStore.refrescar();
  }

  cerrarNotificaciones(event: MouseEvent) {
    event.stopPropagation();
    this.showNotifications.set(false);
    this.expandedNotificationId.set(null);
  }

  abrirNotificacion(notificacion: Notificacion) {
    if (!notificacion.Leido) {
      this.notifStore.marcarLeida(notificacion.Id);
    }
    this.expandedNotificationId.set(
      this.expandedNotificationId() === notificacion.Id
        ? null
        : notificacion.Id,
    );
  }

  alternarDetalleNotificacion(notificacion: Notificacion, event: MouseEvent) {
    event.stopPropagation();
    this.expandedNotificationId.update((id) =>
      id === notificacion.Id ? null : notificacion.Id,
    );
  }

  tieneDetalleOrganizado(notificacion: Notificacion): boolean {
    return this.obtenerDetalleOrganizado(notificacion) !== null;
  }

  obtenerDetalleOrganizado(notificacion: Notificacion): {
    observacion: string | null;
    equipos: { codigo: string; nombre: string; estado: string }[];
    motivo: string | null;
    emisor: string | null;
    fecha: string | null;
  } | null {
    if (!notificacion.Detalle) return null;

    const parsed = this.parseJsonDetalle(notificacion.Detalle);

    if (!parsed || typeof parsed !== 'object') return null;

    const data = parsed as Record<string, unknown>;
    const equipos = Array.isArray(data['equipos'])
      ? data['equipos']
          .filter((item): item is Record<string, unknown> => !!item)
          .map((item) => ({
            codigo: String(item['codigo'] ?? ''),
            nombre: String(item['nombre'] ?? 'Equipo'),
            estado: this.formatearEstadoEquipo(String(item['estado'] ?? '')),
          }))
      : [];

    return {
      observacion:
        typeof data['observacion'] === 'string' ? data['observacion'] : null,
      equipos,
      motivo: typeof data['motivo'] === 'string' ? data['motivo'] : null,
      emisor:
        typeof data['emisor'] === 'string'
          ? data['emisor']
          : typeof data['origen'] === 'string'
            ? data['origen']
            : null,
      fecha: typeof data['fecha'] === 'string' ? data['fecha'] : null,
    };
  }

  obtenerDetalleTexto(notificacion: Notificacion): string | null {
    if (!notificacion.Detalle) return null;
    if (this.tieneDetalleOrganizado(notificacion)) return null;

    return notificacion.Detalle;
  }

  marcarTodasLeidas() {
    this.notifStore.marcarTodasLeidas();
  }

  toggleSidebar() {
    this.showNotifications.set(false);
    this.showUserMenu.set(false);
    this.sidebarService.toggle();
  }

  esAdministrador(): boolean {
    return this.usuario.obtenerRol() === 'administrador';
  }

  seleccionarTabNotificaciones(tab: 'usuario' | 'admin'): void {
    this.activeNotificationTab.set(tab);
    this.expandedNotificationId.set(null);
  }

  notificacionesVisibles(): Notificacion[] {
    if (this.esAdministrador() && this.activeNotificationTab() === 'admin')
      return this.notifStore.notificacionesAdmin();

    return this.notifStore.notificacionesUsuario();
  }

  noLeidasVisibles(): number {
    if (this.esAdministrador() && this.activeNotificationTab() === 'admin')
      return this.notifStore.noLeidasAdmin();

    return this.notifStore.noLeidasUsuario();
  }

  mostrarcarrito() {
    if (this.usuario.estaVacio()) {
      this.router.navigate(['/login']);
    } else {
      this.router.navigate(['/cart']);
    }
  }

  goBack() {
    this.location.back();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!(event.target instanceof Element)) return;

    const target = event.target;

    if (this.showUserMenu() && !target.closest('.user-menu')) {
      this.showUserMenu.set(false);
    }

    if (this.showNotifications() && !target.closest('.notif')) {
      this.showNotifications.set(false);
    }

    if (
      this.sidebarService.isOpen() &&
      !target.closest('.menu-button') &&
      !target.closest('.sidebar-menu')
    ) {
      this.sidebarService.close();
    }
  }

  @HostListener('window:focus')
  onWindowFocus() {
    this.notifStore.refrescar();
  }

  private parseJsonDetalle(value: string): unknown | null {
    return parseJsonResult<unknown>(value).unwrapOr(null);
  }

  private formatearEstadoEquipo(estado: string): string {
    return estado.replace(/_/g, ' ');
  }
}
