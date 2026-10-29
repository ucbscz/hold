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
});
