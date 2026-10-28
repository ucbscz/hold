export interface EntidadFormulario {
  nombre: string;
  femenino?: boolean;
}

export interface TextosFormulario {
  titulo: string;
  boton: string;
  confirmacion: string;
  exito: string;
  fallo: string;
}

export function textosFormulario(
  { nombre, femenino = false }: EntidadFormulario,
  editando: boolean,
): TextosFormulario {
  const articulo = femenino ? 'la' : 'el';
  const terminacion = femenino ? 'a' : 'o';
  const nombreTitulo = capitalizar(nombre);
  const accion = editando ? 'actualizar' : 'registrar';
  return {
    titulo: `${editando ? 'Editar' : 'Registrar'} ${nombre}`,
    boton: editando ? 'Guardar cambios' : `Registrar ${nombre}`,
    confirmacion: editando
      ? `¿Desea guardar los cambios ${femenino ? 'de la' : 'del'} ${nombre}?`
      : `¿Desea registrar ${articulo} ${nombre}?`,
    exito: `${nombreTitulo} ${editando ? 'actualizad' : 'registrad'}${terminacion} con éxito.`,
    fallo: `No se pudo ${accion} ${articulo} ${nombre}.`,
  };
}

function capitalizar(texto: string): string {
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}
