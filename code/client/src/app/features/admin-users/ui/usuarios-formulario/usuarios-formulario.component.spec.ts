import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Usuario, UsuarioServiceAPI } from '@entities/user';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { NEVER } from 'rxjs';
import { UsuariosFormularioComponent } from './usuarios-formulario.component';

describe('UsuariosFormularioComponent', () => {
  let component: UsuariosFormularioComponent;
  let fixture: ComponentFixture<UsuariosFormularioComponent>;

  const crear = (registro?: Usuario) => {
    fixture = TestBed.createComponent(UsuariosFormularioComponent);
    component = fixture.componentInstance;
    if (registro) fixture.componentRef.setInput('registro', registro);
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [UsuariosFormularioComponent],
      }),
    ).compileComponents();
  });

  it('keeps server errors inside the form and lets the user dismiss them without submitting', async () => {
    crear();
    fixture.autoDetectChanges();
    component.mensajeerror = 'El correo ya está registrado.';
    component.error.set(true);
    await fixture.whenStable();
    const error = fixture.nativeElement.querySelector('form app-mostrarerror');
    expect(error).not.toBeNull();
    expect(error.textContent).toContain('El correo ya está registrado.');
    expect(error.querySelector('.toast')).toBeNull();
    expect(getComputedStyle(error).position).toBe('static');
    const close = error.querySelector('button');
    expect(close.type).toBe('button');
    close.click();
    await fixture.whenStable();
    expect(component.error()).toBeFalse();
    expect(component.aviso()).toBeFalse();
  });

  it('prevents creating an account with a mismatched password, even after the confirmation opens', () => {
    crear();
    const save = spyOn(
      TestBed.inject(UsuarioServiceAPI),
      'registrarCuenta',
    ).and.returnValue(NEVER);
    component.contrasena = 'Clave123!';
    component.repetirContrasena = 'Otra123!';
    component.validarEnvio();
    component.confirmar();
    expect(component.aviso()).toBeFalse();
    expect(save).not.toHaveBeenCalled();
    component.repetirContrasena = component.contrasena;
    component.validarEnvio();
    expect(component.aviso()).toBeTrue();
    component.repetirContrasena = '';
    component.confirmar();
    expect(save).not.toHaveBeenCalled();
    component.repetirContrasena = component.contrasena;
    component.confirmar();
    expect(save).toHaveBeenCalledTimes(1);
  });

  it('requires a password for new accounts', () => {
    crear();
    const save = spyOn(
      TestBed.inject(UsuarioServiceAPI),
      'registrarCuenta',
    ).and.returnValue(NEVER);
    component.confirmar();
    expect(save).not.toHaveBeenCalled();
  });

  it('shows the carnet field only when creating', () => {
    crear();
    expect(fixture.nativeElement.querySelector('#carnet')).not.toBeNull();

    crear({ carnet: '12891161', nombre: 'Ana' } as Usuario);
    expect(fixture.nativeElement.querySelector('#carnet')).toBeNull();
  });

  it('prevents editing with a mismatched password', () => {
    crear({ carnet: '12891161' } as Usuario);
    const save = spyOn(
      TestBed.inject(UsuarioServiceAPI),
      'editarUsuario',
    ).and.returnValue(NEVER);
    component.contrasena = 'Clave123!';
    component.repetirContrasena = 'Otra123!';
    component.confirmar();
    expect(save).not.toHaveBeenCalled();
  });

  it('keeps the existing password when both fields are empty', () => {
    crear({ carnet: '12891161' } as Usuario);
    const save = spyOn(
      TestBed.inject(UsuarioServiceAPI),
      'editarUsuario',
    ).and.returnValue(NEVER);
    component.confirmar();
    expect(save).toHaveBeenCalledWith(component.modelo, '');
  });

  it('toggles each password independently without submitting or changing its value', async () => {
    crear();
    fixture.autoDetectChanges();
    await fixture.whenStable();
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
});
