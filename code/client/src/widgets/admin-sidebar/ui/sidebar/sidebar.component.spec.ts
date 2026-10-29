import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { SidebarComponent } from './sidebar.component';
describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [SidebarComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('keeps the desktop navigation open after selection', () => {
    spyOnProperty(window, 'innerWidth').and.returnValue(1200);
    component.groups = [
      { label: 'Préstamos y personas', items: ['Prestamos', 'Usuarios'] },
    ];
    component.activeItem = 'Prestamos';
    component.sidebarService.isOpen.set(true);
    spyOn(component.item, 'emit');
    fixture.detectChanges();

    const aside = fixture.nativeElement.querySelector('aside');
    const buttons = aside.querySelectorAll('.item');
    expect(aside).not.toBeNull();
    expect(buttons.length).toBe(2);
    expect(aside.textContent).not.toContain('Gestión del sistema');
    expect(buttons[0].getAttribute('aria-current')).toBe('page');

    buttons[1].click();

    expect(component.item.emit).toHaveBeenCalledWith('Usuarios');
    expect(component.sidebarService.isOpen()).toBeTrue();
  });
});
