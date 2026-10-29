import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { EmpresasMantenimientoFormularioComponent } from './empresas-mantenimiento-formulario.component';

describe('EmpresasMantenimientoFormularioComponent', () => {
  let fixture: ComponentFixture<EmpresasMantenimientoFormularioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [EmpresasMantenimientoFormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(EmpresasMantenimientoFormularioComponent);
    fixture.detectChanges();
  });

  it('opens in create mode without a record', () => {
    expect(fixture.componentInstance.editando).toBeFalse();
  });
});
