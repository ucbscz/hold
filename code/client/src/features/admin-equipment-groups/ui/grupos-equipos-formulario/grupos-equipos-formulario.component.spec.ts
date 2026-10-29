import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { GruposEquiposFormularioComponent } from './grupos-equipos-formulario.component';

describe('GruposEquiposFormularioComponent', () => {
  let fixture: ComponentFixture<GruposEquiposFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [GruposEquiposFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(GruposEquiposFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
