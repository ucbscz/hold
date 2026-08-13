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

  it('configures the date-time picker in 24-hour, 30-minute intervals', () => {
    expect(component.opcionesInicio.enableTime).toBeTrue();
    expect(component.opcionesInicio.time_24hr).toBeTrue();
    expect(component.opcionesInicio.minuteIncrement).toBe(30);
    expect(component.opcionesInicio.dateFormat).toBe('Y-m-d H:i');
  });
});
