import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ComponentesFormularioComponent } from './componentes-formulario.component';

describe('ComponentesFormularioComponent', () => {
  let fixture: ComponentFixture<ComponentesFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [ComponentesFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(ComponentesFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
