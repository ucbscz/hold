import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { UsuarioServiceAPI } from '@entities/user';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ToastService } from '@shared/ui';
import { of, throwError } from 'rxjs';
import { RecuperarContrasenaComponent } from './recuperar-contrasena.component';

describe('RecuperarContrasenaComponent', () => {
  let component: RecuperarContrasenaComponent;
  let users: UsuarioServiceAPI;
  let toast: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [RecuperarContrasenaComponent],
      }),
    ).compileComponents();

    component = TestBed.createComponent(
      RecuperarContrasenaComponent,
    ).componentInstance;
    users = TestBed.inject(UsuarioServiceAPI);
    toast = TestBed.inject(ToastService);
  });

  it('asks for the recovery code after sending the email', () => {
    spyOn(users, 'solicitarRecuperacionContrasena').and.returnValue(of({}));
    component.email = 'usuario@ucb.edu.bo';

    component.solicitar();

    expect(component.codigoEnviado).toBeTrue();
    expect(toast.kind()).toBe('success');
    expect(toast.message()).toContain('Ingresa el código');
  });

  it('returns to login with an error toast when the email does not exist', () => {
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigate').and.resolveTo(true);
    spyOn(users, 'solicitarRecuperacionContrasena').and.returnValue(
      throwError(() => ({ status: 404 })),
    );
    component.email = 'desconocido@ucb.edu.bo';

    component.solicitar();

    expect(navigate).toHaveBeenCalledOnceWith(['/login']);
    expect(toast.kind()).toBe('error');
    expect(toast.message()).toContain('No encontramos');
  });
});
