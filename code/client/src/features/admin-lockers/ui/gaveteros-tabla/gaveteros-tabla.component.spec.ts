import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { GaveterosTablaComponent } from './gaveteros-tabla.component';
describe('GaveterosTablaComponent', () => {
  let component: GaveterosTablaComponent;
  let fixture: ComponentFixture<GaveterosTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [GaveterosTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(GaveterosTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
