import { Component, ViewChild } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NgForm } from '@angular/forms';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CustomSelectComponent } from '@shared/ui';
import { ValidatedFormsModule } from './validated-forms.module';

@Component({
  imports: [ValidatedFormsModule, CustomSelectComponent],
  template: `<form #form="ngForm">
    <input
      name="nombre"
      [(ngModel)]="nombre"
      required
      minlength="3"
      aria-describedby="help"
    />
    <span id="help">Nombre del equipo</span>
    <input name="email" type="email" [(ngModel)]="email" />
    <label
      ><span class="config-field__control"
        ><input
          name="cost"
          type="number"
          min="0"
          step="1"
          [(ngModel)]="cost"
          required
        /><span>Bs</span></span
      ></label
    >
    @if (showSelect) {
      <app-custom-select
        name="role"
        required
        [(ngModel)]="role"
        [opciones]="['Docente']"
      />
    }
  </form>`,
})
class HostComponent {
  @ViewChild(NgForm) form!: NgForm;
  nombre = '';
  email = '';
  cost: number | null = 0;
  role = '';
  showSelect = true;
}

describe('Inline form validation', () => {
  let fixture: ComponentFixture<HostComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({ imports: [HostComponent] }),
    ).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.autoDetectChanges();
    await fixture.whenStable();
  });
  const input = (fixture: ComponentFixture<HostComponent>, name: string) =>
    fixture.nativeElement.querySelector(`[name="${name}"]`) as HTMLInputElement;
  async function type(name: string, value: string) {
    const field = input(fixture, name);
    field.value = value;
    field.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }
  it('stays quiet initially, validates while typing and removes errors when corrected', async () => {
    expect(fixture.nativeElement.querySelector('.field-error')).toBeNull();
    await type('nombre', 'ab');
    expect(fixture.nativeElement.textContent).toContain('al menos 3');
    expect(input(fixture, 'nombre').getAttribute('aria-invalid')).toBe('true');
    expect(input(fixture, 'nombre').getAttribute('aria-describedby')).toContain(
      'help field-error-',
    );
    await type('nombre', 'Equipo');
    expect(input(fixture, 'nombre').getAttribute('aria-invalid')).toBe('false');
    expect(input(fixture, 'nombre').getAttribute('aria-describedby')).toBe(
      'help',
    );
    expect(fixture.nativeElement.querySelector('.field-error')).toBeNull();
  });
  it('validates required fields on blur and rejects whitespace', async () => {
    input(fixture, 'nombre').dispatchEvent(new Event('blur'));
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Completa este campo');
    await type('nombre', '   ');
    expect(
      fixture.componentInstance.form.controls['nombre'].hasError('required'),
    ).toBeTrue();
  });
  it('validates optional email only when filled', async () => {
    await type('email', 'sin-arroba');
    expect(fixture.nativeElement.textContent).toContain('correo válido');
    await type('email', '');
    expect(fixture.componentInstance.form.controls['email'].valid).toBeTrue();
  });
  it('renders numeric errors outside unit wrappers and rejects fractional integers', async () => {
    await type('cost', '-1');
    const wrapper = fixture.nativeElement.querySelector(
      '.config-field__control',
    );
    expect(wrapper.querySelector('.field-error')).toBeNull();
    expect(wrapper.nextElementSibling.textContent).toContain('mínimo es 0');
    await type('cost', '1.5');
    expect(wrapper.nextElementSibling.textContent).toContain('número entero');
    await type('cost', '2');
    expect(fixture.componentInstance.form.controls['cost'].valid).toBeTrue();
  });
  it('supports custom selects and cleans up dynamic errors and subscriptions', async () => {
    fixture.nativeElement.querySelector('.cs-trigger').click();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Completa este campo');
    fixture.componentInstance.showSelect = false;
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.field-error')).toBeNull();
  });
  it('removes feedback after form reset', async () => {
    await type('nombre', 'a');
    fixture.componentInstance.form.resetForm();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.field-error')).toBeNull();
  });
});
