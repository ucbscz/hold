import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CarrerasFormularioComponent } from './carreras-formulario.component';

describe('CarrerasFormularioComponent', () => {
  let fixture: ComponentFixture<CarrerasFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [CarrerasFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(CarrerasFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });

  it('keeps the career code in uppercase letters and digits', () => {
    const component = fixture.componentInstance;
    component.modelo.Nombre = 'Ingeniería Mecatrónica';

    component.normalizarCodigo('i-mt 2');

    expect(component.modelo.Codigo).toBe('IMT2');
    expect(component['validar']()).toBeNull();
    component.normalizarCodigo('i');
    expect(component['validar']()).toContain('código');
  });
});
