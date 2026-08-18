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

    const search = document.body.querySelector(
      '.cs-search__input',
    ) as HTMLInputElement;
    search.value = 'calibracion';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const labels = Array.from(document.body.querySelectorAll('.cs-item')).map(
      (item) => item.textContent?.trim(),
    );
    expect(labels).toEqual(['Calibración']);
  });

  it('should render its menu outside transformed form containers', () => {
    fixture.detectChanges();

    const menu = document.body.querySelector('.cs-menu');
    expect(menu?.parentElement).toBe(document.body);

    fixture.destroy();
    expect(menu?.isConnected).toBeFalse();
  });
});
