import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { EquiposFormularioComponent } from './equipos-formulario.component';

describe('EquiposFormularioComponent', () => {
  let fixture: ComponentFixture<EquiposFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [EquiposFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(EquiposFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
