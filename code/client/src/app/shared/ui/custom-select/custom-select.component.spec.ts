import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { CustomSelectComponent } from './custom-select.component';

describe('CustomSelectComponent', () => {
  let component: CustomSelectComponent;
  let fixture: ComponentFixture<CustomSelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomSelectComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomSelectComponent);
    component = fixture.componentInstance;
  });

  it('should show search for long option lists and filter labels', () => {
    component.opciones = [
      'Preventivo',
      'Correctivo',
      'Predictivo',
      'Inspección',
      'Calibración',
      'Actualización',
    ];
    fixture.detectChanges();

    fixture.debugElement.query(By.css('.cs-trigger')).nativeElement.click();
    fixture.detectChanges();

    const search = fixture.debugElement.query(By.css('.cs-search__input'))
      .nativeElement as HTMLInputElement;
    search.value = 'calibracion';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const labels = fixture.debugElement
      .queryAll(By.css('.cs-item'))
      .map((item) => item.nativeElement.textContent.trim());
    expect(labels).toEqual(['Calibración']);
  });
});
