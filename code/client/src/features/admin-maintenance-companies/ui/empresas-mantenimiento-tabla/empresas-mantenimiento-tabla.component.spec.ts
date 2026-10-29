import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { EmpresasMantenimientoTablaComponent } from './empresas-mantenimiento-tabla.component';
describe('EmpresasMantenimientoTablaComponent', () => {
  let component: EmpresasMantenimientoTablaComponent;
  let fixture: ComponentFixture<EmpresasMantenimientoTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [EmpresasMantenimientoTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(EmpresasMantenimientoTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
