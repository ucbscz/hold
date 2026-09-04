import { routes } from './app.routes';

describe('application routes', () => {
  const expectedPaths = [
    '',
    'login',
    'inicio',
    'equipo/:id',
    'carrito',
    'perfil',
    'administracion',
    'reserva',
    'destino',
    'historial',
    'registro',
    'terminos',
    'verificar',
  ];

  it('uses the documented lowercase route contract', () => {
    expect(routes.map((route) => route.path)).toEqual(expectedPaths);
    expect(routes[0].redirectTo).toBe('/login');
  });

  it('does not expose legacy mixed-case routes', () => {
    const legacyPaths = [
      'Iniciar-Sesion',
      'Registrar-Usuario',
      'Carrito',
      'Perfil',
      'Formulario',
      'Historial',
      'ConfirmarReserva',
      'loan-history',
      'home',
      'equipment/:id',
      'cart',
      'profile',
      'admin',
      'reservation',
      'history',
      'sign-up',
      'pruebas',
    ];

    expect(routes.some((route) => legacyPaths.includes(route.path ?? ''))).toBe(
      false,
    );
  });

  it('uses one-word routes without hyphens', () => {
    const publicPaths = routes
      .map((route) => route.path ?? '')
      .filter((path) => path.length > 0);

    expect(publicPaths.every((path) => !path.includes('-'))).toBe(true);
  });
});
