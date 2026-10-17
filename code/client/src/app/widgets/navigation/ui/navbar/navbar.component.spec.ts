import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { Notificacion } from '@entities/notification';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { NavbarComponent } from './navbar.component';
@Component({ template: '' })
class RoutePageComponent {}

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [NavbarComponent],
        providers: [
          provideRouter([
            { path: '', redirectTo: '/login', pathMatch: 'full' },
            { path: 'login', component: RoutePageComponent },
            { path: 'registro', component: RoutePageComponent },
            { path: 'inicio', component: RoutePageComponent },
          ]),
        ],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('hides navigation controls before the first navigation resolves', () => {
    expect(fixture.nativeElement.querySelector('.icon-button')).toBeNull();
  });

  for (const url of ['/', '/login', '/registro']) {
    it(`keeps navigation controls hidden on ${url}`, async () => {
      await TestBed.inject(Router).navigateByUrl(url);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.icon-button')).toBeNull();
    });
  }

  it('shows catalog controls and hides them on return to login', async () => {
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/inicio');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.cart-button')).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector('.notif__button'),
    ).not.toBeNull();

    await router.navigateByUrl('/login');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.icon-button')).toBeNull();
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
