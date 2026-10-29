import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { Aviso } from './aviso.component';
describe('AvisoCancelarComponent', () => {
  let component: Aviso;
  let fixture: ComponentFixture<Aviso>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [Aviso],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(Aviso);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
