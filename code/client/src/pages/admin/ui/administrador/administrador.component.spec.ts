import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AdministradorComponent } from './administrador.component';
import { UsuarioService } from '@entities/user';
describe('AdministradorComponent', () => {
  let component: AdministradorComponent;
  let fixture: ComponentFixture<AdministradorComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [AdministradorComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(AdministradorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows only permitted sections to a laboratory administrator', () => {
    const userService = TestBed.inject(UsuarioService);
    spyOn(userService, 'estaVacio').and.returnValue(false);
    spyOn(userService, 'obtenerUsuario').and.returnValue({
      nombre: 'Admin',
      rol: 'administrador_laboratorio',
    });

    component.ngOnInit();

    expect(component.navigationGroups.flatMap((group) => group.items)).toEqual([
      'Prestamos',
      'Usuarios',
    ]);
  });
});
