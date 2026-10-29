import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { UsuarioPrevioComponent } from './usuario-previo.component';
describe('UsuarioPrevioComponent', () => {
  let component: UsuarioPrevioComponent;
  let fixture: ComponentFixture<UsuarioPrevioComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [UsuarioPrevioComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(UsuarioPrevioComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
