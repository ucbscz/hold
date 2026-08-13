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
    expect(component.horas).toHaveSize(48);
    expect(component.horas[0].valor).toBe('00:00');
    expect(component.horas[47].valor).toBe('23:30');
    expect(fixture.nativeElement.querySelectorAll('.availability-selector__day').length).toBeGreaterThan(27);
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
});
