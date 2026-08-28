import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UsuarioServiceAPI } from '@entities/user';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { NEVER } from 'rxjs';
import { IniciarSesionComponent } from './iniciar-sesion.component';
describe('IniciarSesionComponent', () => {
  let component: IniciarSesionComponent;
  let fixture: ComponentFixture<IniciarSesionComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [IniciarSesionComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(IniciarSesionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should ignore repeated submissions while login is pending', () => {
    const api = TestBed.inject(UsuarioServiceAPI);
    const login = spyOn(api, 'iniciarSesion').and.returnValue(NEVER);
    component.email = 'usuario@ucb.edu.bo';
    component.contrasena = 'Password@1';

    component.login();
    component.login();

    expect(login).toHaveBeenCalledTimes(1);
    expect(component.loading).toBeTrue();
  });
});
