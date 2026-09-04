import { Routes } from '@angular/router';
import { adminGuard } from '@app/providers/guards/admin.guard';
import { authGuard } from '@app/providers/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('@pages/sign-in').then((page) => page.IniciarSesionComponent),
  },
  {
    path: 'inicio',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/home').then((page) => page.PantallaMainComponent),
  },
  {
    path: 'equipo/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/equipment-detail').then((page) => page.ObjetoComponent),
  },
  {
    path: 'carrito',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/cart').then((page) => page.CarritoComponent),
  },
  {
    path: 'perfil',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/profile').then((page) => page.PerfilComponent),
  },
  {
    path: 'administracion',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('@pages/admin').then((page) => page.AdministradorComponent),
  },
  {
    path: 'reserva',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/reservation-form').then(
        (page) => page.FormularioComponent,
      ),
  },
  {
    path: 'destino',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/reservation-purpose').then(
        (page) => page.ReservationPurposeComponent,
      ),
  },
  {
    path: 'historial',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/loan-history').then((page) => page.HistorialComponent),
  },
  {
    path: 'registro',
    loadComponent: () =>
      import('@pages/sign-up').then((page) => page.RegistrarUsuarioComponent),
  },
  {
    path: 'terminos',
    loadComponent: () =>
      import('@pages/terms').then((page) => page.TerminosComponent),
  },
  {
    path: 'verificar',
    loadComponent: () =>
      import('@pages/email-verification').then(
        (page) => page.VerificarCorreoComponent,
      ),
  },
];
