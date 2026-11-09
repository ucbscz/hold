import { TestBed } from '@angular/core/testing';
import { TextoExpandibleComponent } from './texto-expandible.component';

describe('TextoExpandibleComponent', () => {
  function crear(texto: string) {
    const fixture = TestBed.createComponent(TextoExpandibleComponent);
    fixture.componentInstance.texto = texto;
    fixture.componentInstance.titulo = 'Adaptador';
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('opens a dialog with the full text when the summary is pressed', () => {
    const elemento = crear('Texto largo completo');
    const dialogo = elemento.querySelector('dialog')!;
    expect(dialogo.open).toBeFalse();

    elemento.querySelector<HTMLButtonElement>('.resumen')!.click();

    expect(dialogo.open).toBeTrue();
    expect(dialogo.querySelector('h2')!.textContent).toContain('Adaptador');
    expect(dialogo.querySelector('p')!.textContent).toContain(
      'Texto largo completo',
    );
    dialogo.querySelector<HTMLButtonElement>('.modal-close-btn')!.click();
    expect(dialogo.open).toBeFalse();
  });

  it('renders no summary button when there is no text', () => {
    expect(crear('').querySelector('.resumen')).toBeNull();
  });
});
