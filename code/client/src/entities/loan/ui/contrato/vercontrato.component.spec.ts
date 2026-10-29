import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { VercontratoComponent } from './vercontrato.component';
describe('VercontratoComponent', () => {
  let component: VercontratoComponent;
  let fixture: ComponentFixture<VercontratoComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [VercontratoComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(VercontratoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('renders outside transformed history cards and cleans up on close', () => {
    const host = fixture.nativeElement as HTMLElement;
    expect(host.parentElement).toBe(document.body);
    fixture.destroy();
    expect(host.isConnected).toBeFalse();
  });
  it('does not render broken carnet images from incomplete contracts', () => {
    const normalize = (
      component as unknown as {
        normalizarContratoHtml: (html: string) => string;
      }
    ).normalizarContratoHtml.bind(component);
    const html = `
      <div class="contract-identity">
        <strong>Carnet de identidad</strong>
        <table><tr>
          <td><img data-carnet="frente" /></td>
          <td><img data-carnet="atras" src="" /></td>
        </tr></table>
      </div>`;

    const result = normalize(html);

    expect(result).toContain('Carnet no adjunto.');
    expect(result).not.toContain('<img');
  });
});
