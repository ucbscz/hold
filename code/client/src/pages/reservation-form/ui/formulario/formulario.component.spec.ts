import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { FormularioComponent } from './formulario.component';
describe('FormularioComponent', () => {
  let component: FormularioComponent;
  let fixture: ComponentFixture<FormularioComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [FormularioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(FormularioComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('replaces missing carnet images with a clear pending state', () => {
    const render = (
      component as unknown as {
        renderizarCarnetEnContrato: (html: string) => string;
      }
    ).renderizarCarnetEnContrato.bind(component);
    const html = `
      <div class="contract-identity">
        <strong>Carnet de identidad</strong>
        <table><tr><td><img data-carnet="frente" src="" /></td></tr></table>
      </div>`;

    const result = render(html);

    expect(result).toContain('Carnet pendiente de adjuntar.');
    expect(result).not.toContain('<img');
  });
  it('keeps both carnet images when they have been attached', () => {
    const image = 'data:image/png;base64,AA==';
    component.carnetFrente = image;
    component.carnetAtras = image;
    const render = (
      component as unknown as {
        renderizarCarnetEnContrato: (html: string) => string;
      }
    ).renderizarCarnetEnContrato.bind(component);
    const html = `
      <div class="contract-identity">
        <table><tr>
          <td><img data-carnet="frente" src="${image}" /></td>
          <td><img data-carnet="atras" src="${image}" /></td>
        </tr></table>
      </div>`;

    expect(render(html).match(/<img/g)?.length).toBe(2);
  });
});
