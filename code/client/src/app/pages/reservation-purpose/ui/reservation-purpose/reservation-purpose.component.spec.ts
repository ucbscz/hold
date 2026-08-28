import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ReservationPurposeComponent } from './reservation-purpose.component';

describe('ReservationPurposeComponent', () => {
  let component: ReservationPurposeComponent;
  let fixture: ComponentFixture<ReservationPurposeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({ imports: [ReservationPurposeComponent] }),
    ).compileComponents();

    fixture = TestBed.createComponent(ReservationPurposeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
