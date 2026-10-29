import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { MueblesFormularioComponent } from './muebles-formulario.component';

describe('MueblesFormularioComponent', () => {
  let fixture: ComponentFixture<MueblesFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [MueblesFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(MueblesFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
