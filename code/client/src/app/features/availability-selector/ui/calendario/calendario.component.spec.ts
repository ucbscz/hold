import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CalendarioComponent } from './calendario.component';
describe('CalendarioComponent', () => {
  let component: CalendarioComponent;
  let fixture: ComponentFixture<CalendarioComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [CalendarioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(CalendarioComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders an in-place calendar with 30-minute time options', () => {
    expect(component.diasDelMes.length % 7).toBe(0);
    expect(component.horas).toHaveSize(41);
    expect(component.horas[0].value).toBe('00:00');
    expect(component.horas[40].value).toBe('20:00');
    expect(
      fixture.nativeElement.querySelectorAll('.availability-selector__day')
        .length,
    ).toBeGreaterThan(27);
  });

  it('keeps the minimum 30-minute duration when the start time changes', () => {
    const inicio = new Date(2030, 0, 10, 8, 0);
    const fin = new Date(2030, 0, 10, 8, 30);
    component.fechaInicioSeleccionada.set(inicio);
    component.fechaFinSeleccionada.set(fin);

    component.cambiarHora('inicio', '09:30');

    expect(component.fechaInicioSeleccionada()?.getHours()).toBe(9);
    expect(component.fechaInicioSeleccionada()?.getMinutes()).toBe(30);
    expect(component.fechaFinSeleccionada()?.getHours()).toBe(10);
    expect(component.fechaFinSeleccionada()?.getMinutes()).toBe(0);
  });

  it('does not allow a start time later than 19:30', () => {
    expect(component.horaDeshabilitada('inicio', '20:00')).toBeTrue();
    expect(component.horaDeshabilitada('inicio', '19:30')).toBeFalse();
  });

  it('disables dates and times beyond the selected groups maximum duration', () => {
    component.entradaCarrito = {
      1: {
        nombre: 'Equipo',
        modelo: '',
        marca: '',
        cantidad: 1,
        fecha_inicio: null,
        fecha_final: null,
        imagen: '',
        precio: 0,
        cantidadMax: 1,
        tiempoMaximoPrestamoDias: 1,
      },
    };
    const inicio = new Date(2030, 0, 10, 8, 0);
    component.fechaInicioSeleccionada.set(inicio);
    component.fechaFinSeleccionada.set(new Date(2030, 0, 11, 8, 0));
    component.seleccionarCampo('fin');

    expect(component.rangoValido).toBeTrue();
    expect(component.esDiaDeshabilitado(new Date(2030, 0, 12))).toBeTrue();
    expect(component.horaDeshabilitada('fin', '08:30')).toBeTrue();
  });
});
