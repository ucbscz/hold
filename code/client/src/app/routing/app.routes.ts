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
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/home').then((page) => page.PantallaMainComponent),
  },
  {
    path: 'equipment/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/equipment-detail').then((page) => page.ObjetoComponent),
  },
  {
    path: 'cart',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/cart').then((page) => page.CarritoComponent),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/profile').then((page) => page.PerfilComponent),
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('@pages/admin').then((page) => page.AdministradorComponent),
  },
  {
    path: 'reservation',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/reservation-form').then(
        (page) => page.FormularioComponent,
      ),
  },
  {
    path: 'loan-history',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/loan-history').then((page) => page.HistorialComponent),
  },
  {
    path: 'sign-up',
    loadComponent: () =>
      import('@pages/sign-up').then((page) => page.RegistrarUsuarioComponent),
  },
];
