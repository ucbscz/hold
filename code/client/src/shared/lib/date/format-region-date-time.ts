import { REGION } from '@region/region';

const regionDateTimeFormatter = new Intl.DateTimeFormat(REGION.locale, {
  timeZone: REGION.timeZone,
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

export function formatRegionDateTime(
  value: Date | string | null | undefined,
): string {
  if (!value) return '';

  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime())
    ? ''
    : regionDateTimeFormatter.format(date);
}
