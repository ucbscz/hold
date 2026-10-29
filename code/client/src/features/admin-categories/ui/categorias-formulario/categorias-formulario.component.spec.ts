import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CategoriasFormularioComponent } from './categorias-formulario.component';

describe('CategoriasFormularioComponent', () => {
  let fixture: ComponentFixture<CategoriasFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [CategoriasFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(CategoriasFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
