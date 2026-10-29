import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AvisoEliminarComponent } from './aviso-eliminar.component';
describe('AvisoEliminarComponent', () => {
  let component: AvisoEliminarComponent;
  let fixture: ComponentFixture<AvisoEliminarComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [AvisoEliminarComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(AvisoEliminarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
