import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PrestamoDto } from '@entities/admin';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { RecursosPrestamoComponent } from './recursos-prestamo.component';

describe('RecursosPrestamoComponent', () => {
  let fixture: ComponentFixture<RecursosPrestamoComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({ imports: [RecursosPrestamoComponent] }),
    ).compileComponents();
    fixture = TestBed.createComponent(RecursosPrestamoComponent);
  });
  for (const estado of [
    'pendiente',
    'aprobado',
    'activo',
    'atrasado',
    'finalizado',
    'cancelado',
    'rechazado',
  ]) {
    it(`shows an existing contract in ${estado} history`, () => {
      fixture.componentInstance.prestamos = [
        Object.assign(new PrestamoDto(), {
          Id: 1,
          EstadoPrestamo: estado,
          IdContrato: '4',
        }),
      ];
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Ver contrato');
      expect(fixture.componentInstance.puedeVerUbicacion).toBe(
        ['activo', 'aprobado'].includes(estado),
      );
    });
  }
  it('does not offer a contract when none exists', () => {
    fixture.componentInstance.prestamos = [new PrestamoDto()];
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Ver contrato');
  });
  it('opens the pickup hierarchy with the responsible full name', () => {
    fixture.componentInstance.prestamos = [
      Object.assign(new PrestamoDto(), {
        EstadoPrestamo: 'activo',
        NombreGrupoEquipo: 'Sensor',
        UbicacionEquipo: 'Laboratorio',
        NombreMueble: 'Mueble 1',
        NombreGavetero: 'Gaveta 2',
        AdministradorAmbiente: 'Fernando Terrazas',
      }),
    ];
    fixture.detectChanges();
    fixture.nativeElement.querySelector('button').click();
    const dialog = fixture.nativeElement.querySelector(
      'dialog',
    ) as HTMLDialogElement;
    expect(dialog.open).toBeTrue();
    for (const text of [
      'Laboratorio',
      'Mueble 1',
      'Gaveta 2',
      'Fernando Terrazas',
    ])
      expect(dialog.textContent).toContain(text);
    dialog.close();
  });
});
