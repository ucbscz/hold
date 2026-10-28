import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AccesoriosFormularioComponent } from './accesorios-formulario.component';

describe('AccesoriosFormularioComponent', () => {
  let fixture: ComponentFixture<AccesoriosFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [AccesoriosFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(AccesoriosFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
