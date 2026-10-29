import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { GaveterosFormularioComponent } from './gaveteros-formulario.component';

describe('GaveterosFormularioComponent', () => {
  let fixture: ComponentFixture<GaveterosFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [GaveterosFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(GaveterosFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
