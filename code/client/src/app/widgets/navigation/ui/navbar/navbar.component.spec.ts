import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Notificacion } from '@entities/notification';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { NavbarComponent } from './navbar.component';
describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [NavbarComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('labels an overdue notification date as the return deadline', () => {
    const notification = new Notificacion();
    notification.Tipo = 'PrestamoAtrasado';
    notification.Detalle = JSON.stringify({
      fecha: '23/08/2026 18:00',
    });

    const detail = component.obtenerDetalleOrganizado(notification);

    expect(detail?.etiquetaFecha).toBe('Fecha límite de devolución');
  });

  it('labels a regular notification detail date as the action date', () => {
    const notification = new Notificacion();
    notification.Detalle = JSON.stringify({ fecha: '22/08/2026 10:00' });

    const detail = component.obtenerDetalleOrganizado(notification);

    expect(detail?.etiquetaFecha).toBe('Fecha de la acción');
  });
});
