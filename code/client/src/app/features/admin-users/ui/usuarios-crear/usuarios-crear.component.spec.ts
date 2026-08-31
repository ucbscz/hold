import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { UsuarioServiceAPI } from '@entities/user';
import { NEVER } from 'rxjs';
import { UsuariosCrearComponent } from './usuarios-crear.component';
describe('UsuariosCrearComponent', () => {
  let component: UsuariosCrearComponent;
  let fixture: ComponentFixture<UsuariosCrearComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [UsuariosCrearComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(UsuariosCrearComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('prevents saving a mismatched password, even after the confirmation opens', () => {
    const save = spyOn(
      TestBed.inject(UsuarioServiceAPI),
      'registrarCuenta',
    ).and.returnValue(NEVER);
    component.contrasena = 'Clave123!';
    component.repetirContrasena = 'Otra123!';
    component.validarcrear();
    component.registrar();
    expect(component.aviso()).toBeFalse();
    expect(save).not.toHaveBeenCalled();
    component.repetirContrasena = component.contrasena;
    component.validarcrear();
    expect(component.aviso()).toBeTrue();
    component.repetirContrasena = '';
    component.registrar();
    expect(save).not.toHaveBeenCalled();
    component.repetirContrasena = component.contrasena;
    component.registrar();
    expect(save).toHaveBeenCalledTimes(1);
  });

  it('toggles each password independently without submitting or changing its value', async () => {
    fixture.autoDetectChanges();
    const save = spyOn(
      TestBed.inject(UsuarioServiceAPI),
      'registrarCuenta',
    ).and.returnValue(NEVER);
    const fields = Array.from(
      fixture.nativeElement.querySelectorAll('app-password-input'),
    ) as HTMLElement[];
    expect(fields.length).toBe(2);
    const input = fields[0].querySelector('input')!;
    const repeat = fields[1].querySelector('input')!;
    input.value = 'Clave123!';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    repeat.value = 'Clave123!';
    repeat.dispatchEvent(new Event('input', { bubbles: true }));
    await fixture.whenStable();
    const toggle = fields[0].querySelector('button')!;
    toggle.click();
    await fixture.whenStable();
    expect(input.type).toBe('text');
    expect(repeat.type).toBe('password');
    expect(input.value).toBe('Clave123!');
    expect(component.contrasena).toBe('Clave123!');
    expect(component.repetirContrasena).toBe('Clave123!');
    expect(toggle.getAttribute('aria-label')).toBe('Ocultar contraseña');
    toggle.click();
    await fixture.whenStable();
    expect(input.type).toBe('password');
    expect(save).not.toHaveBeenCalled();
  });

  it('requires a password for new accounts', () => {
    const save = spyOn(
      TestBed.inject(UsuarioServiceAPI),
      'registrarCuenta',
    ).and.returnValue(NEVER);
    component.registrar();
    expect(save).not.toHaveBeenCalled();
  });
});
