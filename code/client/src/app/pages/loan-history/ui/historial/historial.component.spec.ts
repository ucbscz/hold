import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { HistorialComponent } from './historial.component';
describe('HistorialComponent', () => {
  let component: HistorialComponent;
  let fixture: ComponentFixture<HistorialComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [HistorialComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(HistorialComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('starts with pending loans and has no board tab', () => {
    expect(component.item).toBe('Pendiente');
    expect(fixture.nativeElement.querySelector('app-pendiente')).not.toBeNull();
    const tabs = fixture.nativeElement.querySelector('.history-tabs');
    expect(tabs.querySelector('button').textContent.trim()).toBe('Pendientes');
    expect(tabs.textContent).not.toContain('Tablero');
  });
  it('keeps filters when switching loan states', () => {
    component.filtroTexto = 'Mini Dron';
    component.seleccionarEstado('Activo');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-activo')).not.toBeNull();
    expect(component.filtroTexto).toBe('Mini Dron');
  });
});
