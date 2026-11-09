import { parseJsonResult } from '@shared/lib/result';
import { AuditObservationDetail } from './audit-observation-detail';

const CAMPOS_ESTRUCTURADOS = new Set([
  'observacion',
  'texto',
  'equipos',
  'usuarioNombre',
  'usuarioCarnet',
  'equiposPrestamo',
  'fechaInicio',
  'fechaDevolucion',
  'cambios',
]);

const ETIQUETAS_DATO: Record<string, string> = {
  cuentaRecreada: 'Cuenta recreada',
  aceptoTerminos: 'Aceptó términos',
  versionTerminos: 'Versión de términos',
  fechaAceptacion: 'Fecha de aceptación',
  anterior: 'Valor anterior',
};

export function separarPalabras(texto: string): string {
  return texto
    .replace(/([a-záéíóúñ])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
    .replace(/^./, (value) => value.toUpperCase());
}

export function parseAuditDetail(detalle: string): AuditObservationDetail {
  const parsed = parseJsonResult<unknown>(detalle);

  if (parsed.isOk() && isRecord(parsed.value))
    return normalizarDetalle(parsed.value);

  return parseLegacyLoanDetail(detalle) ?? { texto: detalle };
}

function normalizarDetalle(
  value: Record<string, unknown>,
): AuditObservationDetail {
  const detail = value as unknown as AuditObservationDetail;
  const legacy =
    typeof detail.texto === 'string'
      ? parseLegacyLoanDetail(detail.texto)
      : null;

  if (legacy) return legacy;

  const datos = Object.entries(value)
    .filter(([key]) => !CAMPOS_ESTRUCTURADOS.has(key) && !esClaveSensible(key))
    .map(([key, item]) => ({
      etiqueta: ETIQUETAS_DATO[key] ?? separarPalabras(key),
      valor: formatearDato(item),
    }))
    .filter((item) => item.valor !== '');

  return {
    ...detail,
    datos: datos.length ? datos : undefined,
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

function esClaveSensible(key: string): boolean {
  return /(contrasena|password|token|hash|secret|imagen|firma|identidadexternaid|googleid|refresh)/i.test(
    key,
  );
}

function formatearDato(value: unknown): string {
  if (value === null || value === undefined || value === '') return 'Sin valor';
  if (typeof value === 'boolean') return value ? 'Sí' : 'No';
  if (typeof value === 'string' || typeof value === 'number')
    return String(value);
  if (Array.isArray(value) && value.every((item) => typeof item !== 'object'))
    return value.join(', ');

  return '';
}

function parseLegacyLoanDetail(detail: string): AuditObservationDetail | null {
  const match = detail.match(
    /^Usuario:\s*(.+?)\s*\(([^)]+)\)\.\s*Equipos:\s*(.+?)\.\s*Inicio:\s*(.+?)\s+UTC\.\s*Devolución:\s*(.+?)\s+UTC\.?$/i,
  );

  if (!match) return null;

  return {
    usuarioNombre: match[1].trim(),
    usuarioCarnet: match[2].trim(),
    equiposPrestamo: match[3].trim(),
    fechaInicio: normalizarFechaUtcLegacy(match[4]),
    fechaDevolucion: normalizarFechaUtcLegacy(match[5]),
  };
}

function normalizarFechaUtcLegacy(value: string): string {
  const normalized = value.trim().replace(' ', 'T');
  return normalized.endsWith('Z') ? normalized : `${normalized}:00Z`;
}
