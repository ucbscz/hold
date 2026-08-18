import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { UsuariosTablaComponent } from './usuarios-tabla.component';
describe('UsuariosTablaComponent', () => {
  let component: UsuariosTablaComponent;
  let fixture: ComponentFixture<UsuariosTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [UsuariosTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(UsuariosTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should combine role and career filters', () => {
    component.usuarioscopia = [
      {
        carnet: 'A1',
        nombre: 'Ana',
        rol: 'administrador',
        carrera: 'Ingeniería de Software',
      },
      {
        carnet: 'E1',
        nombre: 'Elena',
        rol: 'estudiante',
        carrera: 'Ingeniería Mecatrónica',
      },
    ];

    component.seleccionarRol('estudiante');
    component.seleccionarCarrera('Ingeniería Mecatrónica');

    expect(component.usuarios.map((usuario) => usuario.carnet)).toEqual(['E1']);
  });
});
