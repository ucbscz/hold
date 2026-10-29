import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '@shared/ui';
import { Observable, of, throwError } from 'rxjs';
import { FormularioAdminComponent } from './formulario-admin';
import { textosFormulario } from './textos-formulario';

interface Registro {
  Id?: number;
  Nombre?: string;
}

@Component({ template: '' })
class FormularioPrueba extends FormularioAdminComponent<Registro> {
  protected readonly entidad = { nombre: 'carrera', femenino: true };
  respuesta: Observable<unknown> = of(null);
  creados: Registro[] = [];
  editados: Registro[] = [];

  protected crearModelo(): Registro {
    return {};
  }

  protected override validar(): string | null {
    return this.modelo.Nombre ? null : 'Nombre requerido.';
  }

  protected crear(registro: Registro) {
    this.creados.push(registro);
    return this.respuesta;
  }

  protected editar(registro: Registro) {
    this.editados.push(registro);
    return this.respuesta;
  }
}

describe('FormularioAdminComponent', () => {
  let formulario: FormularioPrueba;
  let toast: jasmine.SpyObj<ToastService>;

  beforeEach(() => {
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success']);
    TestBed.configureTestingModule({
      providers: [{ provide: ToastService, useValue: toast }],
    });
    formulario = TestBed.runInInjectionContext(() => new FormularioPrueba());
  });

  it('creates a record and notifies the table', () => {
    const guardado = jasmine.createSpy('guardado');
    formulario.guardado.subscribe(guardado);
    formulario.modelo.Nombre = 'Sistemas';

    formulario.validarEnvio();
    formulario.confirmar();

    expect(formulario.creados).toEqual([{ Nombre: 'Sistemas' }]);
    expect(formulario.abierto()).toBeFalse();
    expect(toast.success).toHaveBeenCalledWith('Carrera registrada con éxito.');
    expect(guardado).toHaveBeenCalled();
  });

  it('edits a copy without mutating the original record', () => {
    const original = { Id: 1, Nombre: 'Civil' };
    formulario.registro = original;
    formulario.modelo.Nombre = 'Industrial';

    formulario.confirmar();

    expect(formulario.editando).toBeTrue();
    expect(original.Nombre).toBe('Civil');
    expect(formulario.editados).toEqual([{ Id: 1, Nombre: 'Industrial' }]);
  });

  it('shows the validation message instead of asking for confirmation', () => {
    formulario.validarEnvio();

    expect(formulario.error()).toBeTrue();
    expect(formulario.mensajeerror).toBe('Nombre requerido.');
    expect(formulario.aviso()).toBeFalse();
  });

  it('keeps the form open and shows the server error when saving fails', () => {
    formulario.respuesta = throwError(() => ({ error: 'Duplicado' }));
    formulario.modelo.Nombre = 'Sistemas';

    formulario.confirmar();

    expect(formulario.abierto()).toBeTrue();
    expect(formulario.error()).toBeTrue();
    expect(formulario.procesando).toBeFalse();
  });

  it('ignores a second submit while the first is in progress', () => {
    formulario.respuesta = new Observable();
    formulario.modelo.Nombre = 'Sistemas';

    formulario.confirmar();
    formulario.confirmar();

    expect(formulario.creados.length).toBe(1);
  });
});

describe('textosFormulario', () => {
  it('builds masculine create texts', () => {
    expect(textosFormulario({ nombre: 'grupo de equipo' }, false)).toEqual({
      titulo: 'Registrar grupo de equipo',
      boton: 'Registrar grupo de equipo',
      confirmacion: '¿Desea registrar el grupo de equipo?',
      exito: 'Grupo de equipo registrado con éxito.',
      fallo: 'No se pudo registrar el grupo de equipo.',
    });
  });

  it('builds feminine edit texts', () => {
    expect(
      textosFormulario({ nombre: 'categoría', femenino: true }, true),
    ).toEqual({
      titulo: 'Editar categoría',
      boton: 'Guardar cambios',
      confirmacion: '¿Desea guardar los cambios de la categoría?',
      exito: 'Categoría actualizada con éxito.',
      fallo: 'No se pudo actualizar la categoría.',
    });
  });
});
