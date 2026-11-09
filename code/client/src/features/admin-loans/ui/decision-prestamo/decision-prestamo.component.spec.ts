import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { PrestamosAPIService } from '@entities/loan';
import { of, throwError } from 'rxjs';
import { DecisionPrestamoComponent } from './decision-prestamo.component';

describe('DecisionPrestamoComponent', () => {
  let component: DecisionPrestamoComponent;
  let fixture: ComponentFixture<DecisionPrestamoComponent>;
  let api: PrestamosAPIService;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [DecisionPrestamoComponent],
      }),
    ).compileComponents();

    fixture = TestBed.createComponent(DecisionPrestamoComponent);
    component = fixture.componentInstance;
    api = TestBed.inject(PrestamosAPIService);
  });

  it('sends and preserves an approval annotation through retries', () => {
    component.decision = 'aprobado';
    component.prestamoId = 8;
    const decidido = spyOn(component.decidido, 'emit');
    const fallo = spyOn(component.fallo, 'emit');
    const request = spyOn(api, 'cambiarEstadoPrestamo');
    request.and.returnValue(throwError(() => new Error('Offline')));

    component.detalle = 'Autorizado para la práctica de laboratorio';
    component.confirmar();

    expect(fallo).toHaveBeenCalled();
    expect(component.detalle).toBe(
      'Autorizado para la práctica de laboratorio',
    );
    request.and.returnValue(of({}));
    component.confirmar();
    expect(request).toHaveBeenCalledWith(
      8,
      'aprobado',
      'Autorizado para la práctica de laboratorio',
    );
    expect(decidido).toHaveBeenCalledWith('Préstamo aprobado con éxito.');
  });

  it('requires and sends the rejection reason', () => {
    component.decision = 'rechazado';
    component.prestamoId = 9;
    const decidido = spyOn(component.decidido, 'emit');
    const request = spyOn(api, 'cambiarEstadoPrestamo').and.returnValue(of({}));

    component.confirmar();
    expect(request).not.toHaveBeenCalled();
    component.detalle = 'El equipo está reservado para mantenimiento';
    component.confirmar();

    expect(request).toHaveBeenCalledWith(
      9,
      'rechazado',
      'El equipo está reservado para mantenimiento',
    );
    expect(decidido).toHaveBeenCalledWith('Préstamo rechazado con éxito.');
  });
});
