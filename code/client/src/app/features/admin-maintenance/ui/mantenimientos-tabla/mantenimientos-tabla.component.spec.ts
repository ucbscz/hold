import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { Mantenimientos } from '@entities/admin';
import { MantenimientosAgrupados } from '@entities/maintenance';
import { MantenimientosTablaComponent } from './mantenimientos-tabla.component';
describe('MantenimientosTablaComponent', () => {
  let component: MantenimientosTablaComponent;
  let fixture: ComponentFixture<MantenimientosTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [MantenimientosTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(MantenimientosTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should filter maintenances by start and end dates', () => {
    const insideRange = new Mantenimientos();
    insideRange.Id = 1;
    insideRange.FechaMantenimiento = new Date('2026-08-10T09:00:00');
    insideRange.FechaFinalDeMantenimiento = new Date('2026-08-12T17:00:00');

    const outsideRange = new Mantenimientos();
    outsideRange.Id = 2;
    outsideRange.FechaMantenimiento = new Date('2026-08-01T09:00:00');
    outsideRange.FechaFinalDeMantenimiento = new Date('2026-08-20T17:00:00');

    component.mantenimientos = [
      new MantenimientosAgrupados([insideRange]),
      new MantenimientosAgrupados([outsideRange]),
    ];
    component.fechaInicioDesde = new Date('2026-08-09');
    component.fechaFinHasta = new Date('2026-08-13');

    component.aplicarFiltros();

    expect(
      component.mantenimientosFiltrados.map(
        (mantenimiento) => mantenimiento.datosgrupo.Id,
      ),
    ).toEqual([1]);
  });
});
