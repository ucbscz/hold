import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PrestamoDto } from '@entities/admin';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ActivoComponent } from './activo.component';
describe('ActivoComponent', () => {
  let component: ActivoComponent;
  let fixture: ComponentFixture<ActivoComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [ActivoComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(ActivoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('pagina el historial sin alterar el total filtrado', () => {
    const prestamos = Array.from({ length: 13 }, (_, index) =>
      Object.assign(new PrestamoDto(), {
        Id: index + 1,
        NombreGrupoEquipo: `Equipo ${index + 1}`,
      }),
    );

    component.agruparPrestamos(prestamos);

    expect(component.totalRegistros).toBe(13);
    expect(component.datosPaginados.size).toBe(6);

    component.paginaActual = 3;
    expect(component.datosPaginados.size).toBe(1);
  });
});
