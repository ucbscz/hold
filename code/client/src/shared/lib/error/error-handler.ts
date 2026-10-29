type ErrorRecord = Record<string, unknown>;

export function extractErrorMessage(
  error: unknown,
  defaultMessage: string = 'Error desconocido',
): string {
  if (!error) return defaultMessage;
  if (typeof error === 'string') return error;
  if (!isErrorRecord(error)) return defaultMessage;

  const nestedMessage = extractBodyMessage(error['error']);

  if (nestedMessage) return nestedMessage;

  const bodyMessage = extractBodyMessage(error);

  if (bodyMessage) return bodyMessage;

  const status = typeof error['status'] === 'number' ? error['status'] : null;
  const hasHttpStatusText = typeof error['statusText'] === 'string';

  if (!hasHttpStatusText) {
    const directMessage =
      readString(error, 'message') ?? readString(error, 'mensaje');

    if (directMessage) return directMessage;
  }

  if (status === 0)
    return 'No se pudo conectar con el servidor. Verifica tu conexión.';
  if (status === 404) return 'No se encontró el recurso solicitado.';
  if (status === 401 || status === 403)
    return 'No tienes permisos para realizar esta acción.';
  if (status !== null && status >= 500)
    return 'Error en el servidor. Intenta nuevamente más tarde.';

  return defaultMessage;
}

function extractBodyMessage(body: unknown): string | null {
  if (typeof body === 'string' && body.trim()) return body;
  if (!isErrorRecord(body)) return null;

  return (
    readFirstMessage(body['Errors']) ??
    readFirstMessage(body['errors']) ??
    readFirstMessage(body['ValidationErrors']) ??
    readFirstMessage(body['validationErrors']) ??
    readString(body, 'mensaje') ??
    readString(body, 'message') ??
    readString(body, 'error')
  );
}

function readFirstMessage(value: unknown): string | null {
  if (!Array.isArray(value) || value.length === 0) return null;

  const firstValue = value[0];

  if (typeof firstValue === 'string' && firstValue.trim()) return firstValue;
  if (!isErrorRecord(firstValue)) return null;

  return (
    readString(firstValue, 'description') ?? readString(firstValue, 'message')
  );
}

function readString(source: ErrorRecord, key: string): string | null {
  const value = source[key];

  return typeof value === 'string' && value.trim() ? value : null;
}

function isErrorRecord(value: unknown): value is ErrorRecord {
  return Boolean(value && typeof value === 'object');
}
