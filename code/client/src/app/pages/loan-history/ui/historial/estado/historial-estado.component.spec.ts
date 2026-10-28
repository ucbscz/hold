import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { HistorialEstadoComponent } from './historial-estado.component';

describe('HistorialEstadoComponent', () => {
  let component: HistorialEstadoComponent;
  let fixture: ComponentFixture<HistorialEstadoComponent>;

  const crear = (estado: string) => {
    fixture = TestBed.createComponent(HistorialEstadoComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('estado', estado);
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [HistorialEstadoComponent],
      }),
    ).compileComponents();
  });

  it('shows the empty state configured for the selected status', () => {
    crear('cancelado');

    const vacio = fixture.nativeElement.querySelector('.hist-empty');
    expect(vacio.textContent).toContain('Sin cancelaciones');
  });

  it('allows cancelling only pending or approved loans', () => {
    crear('pendiente');
    expect(component.cancelable).toBeTrue();

    fixture.componentRef.setInput('estado', 'finalizado');
    fixture.detectChanges();
    expect(component.cancelable).toBeFalse();
  });

  it('marks overdue loans', () => {
    crear('atrasado');

    expect(component.atrasado).toBeTrue();
  });
});
