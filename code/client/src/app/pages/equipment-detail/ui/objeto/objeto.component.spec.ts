import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ObjetoComponent } from './objeto.component';
describe('ObjetoComponent', () => {
  let component: ObjetoComponent;
  let fixture: ComponentFixture<ObjetoComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [ObjetoComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(ObjetoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('places technical resources outside the main equipment detail', () => {
    component.cargando = false;
    component.producto.nombre = 'Equipo de prueba';
    component.producto.url_data_sheet = 'https://example.com/ficha.pdf';
    fixture.detectChanges();

    const details = fixture.nativeElement.querySelector('.contenedor-objeto');
    const resources = fixture.nativeElement.querySelector(
      '.equipment-resources',
    );
    expect(resources).not.toBeNull();
    expect(details.contains(resources)).toBeFalse();
    expect(details.nextElementSibling).toBe(resources);
    expect(resources.textContent).toContain('Ver componentes');
    expect(resources.textContent).toContain('Ver ficha técnica');
  });

  it('muestra el acceso para volver al inicio de comentarios cuando corresponde', () => {
    component.cargando = false;
    component.mostrarBotonSubir.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector(
      '.comentarios-subir',
    ) as HTMLButtonElement | null;

    expect(button).not.toBeNull();
    expect(button?.getAttribute('aria-label')).toBe(
      'Volver al inicio de los comentarios',
    );
  });

  it('consulta el lunes al cerrar el sábado', () => {
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date(2026, 7, 22, 19, 0, 0));

    const siguienteHorario = component['siguienteHorarioConsultable']();

    expect(siguienteHorario).toEqual(new Date(2026, 7, 24, 8, 0, 0));
    jasmine.clock().uninstall();
  });
});
