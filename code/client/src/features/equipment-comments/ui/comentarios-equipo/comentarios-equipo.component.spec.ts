import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ComentariosEquipoComponent } from './comentarios-equipo.component';

describe('ComentariosEquipoComponent', () => {
  let component: ComentariosEquipoComponent;
  let fixture: ComponentFixture<ComentariosEquipoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [ComentariosEquipoComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(ComentariosEquipoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('muestra el acceso para volver al inicio de comentarios cuando corresponde', () => {
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

  it('valida el contenido antes de publicar', () => {
    component.grupoId = 3;
    component.comentarioTexto = '   ';
    component.publicarComentario();

    expect(component.comentarioError).toBe(
      'Escribe un comentario antes de publicar.',
    );
  });
});
