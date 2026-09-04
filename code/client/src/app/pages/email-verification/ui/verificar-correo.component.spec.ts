import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { VerificarCorreoComponent } from './verificar-correo.component';

describe('VerificarCorreoComponent', () => {
  let fixture: ComponentFixture<VerificarCorreoComponent>;
  let component: VerificarCorreoComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({ imports: [VerificarCorreoComponent] }),
    ).compileComponents();
    fixture = TestBed.createComponent(VerificarCorreoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('rejects a verification page without token', () => {
    expect(component.estado).toBe('error');
  });
});
