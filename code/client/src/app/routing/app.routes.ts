import { Routes } from '@angular/router';
import { adminGuard } from '@app/providers/guards/admin.guard';
import { authGuard } from '@app/providers/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/Iniciar-Sesion', pathMatch: 'full' },
  {
    path: 'Iniciar-Sesion',
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
    path: 'equipo/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/equipment-detail').then((page) => page.ObjetoComponent),
  },
  {
    path: 'Carrito',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/cart').then((page) => page.CarritoComponent),
  },
  {
    path: 'ConfirmarReserva',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/cart').then((page) => page.CarritoComponent),
  },
  {
    path: 'Perfil',
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
    path: 'Formulario',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/reservation-form').then(
        (page) => page.FormularioComponent,
      ),
  },
  {
    path: 'Historial',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/loan-history').then((page) => page.HistorialComponent),
  },
  {
    path: 'Registrar-Usuario',
    loadComponent: () =>
      import('@pages/sign-up').then((page) => page.RegistrarUsuarioComponent),
  },
  {
    path: 'pruebas',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('@features/admin-search').then(
        (feature) => feature.BuscadorComponent,
      ),
  },
];
