const boliviaDateTimeFormatter = new Intl.DateTimeFormat('es-BO', {
  timeZone: 'America/La_Paz',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

export function formatBoliviaDateTime(
  value: Date | string | null | undefined,
): string {
  if (!value) return '';

  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime())
    ? ''
    : boliviaDateTimeFormatter.format(date);
}
