import { routes } from './app.routes';

describe('application routes', () => {
  const expectedPaths = [
    '',
    'login',
    'home',
    'equipment/:id',
    'cart',
    'profile',
    'admin',
    'reservation',
    'loan-history',
    'sign-up',
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
      'pruebas',
    ];

    expect(routes.some((route) => legacyPaths.includes(route.path ?? ''))).toBe(
      false,
    );
  });
});
