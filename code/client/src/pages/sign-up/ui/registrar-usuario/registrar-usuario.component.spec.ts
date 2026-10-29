import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UsuarioServiceAPI } from '@entities/user';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { RegistrarUsuarioComponent } from './registrar-usuario.component';
describe('RegistrarUsuarioComponent', () => {
  let component: RegistrarUsuarioComponent;
  let fixture: ComponentFixture<RegistrarUsuarioComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [RegistrarUsuarioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(RegistrarUsuarioComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows Google verification before the registration form', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.google-button')).not.toBeNull();
    expect(element.querySelector('.register-form')).toBeNull();
  });

  it('does not submit an account before Google verification', () => {
    const users = TestBed.inject(UsuarioServiceAPI);
    const register = spyOn(users, 'registrarCuenta');

    component.registrar({ invalid: false } as never);

    expect(register).not.toHaveBeenCalled();
  });
});
