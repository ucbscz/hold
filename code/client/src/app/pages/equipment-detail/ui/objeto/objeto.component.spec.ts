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
});
