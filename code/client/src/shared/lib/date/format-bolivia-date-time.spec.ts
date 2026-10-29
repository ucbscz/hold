import { formatBoliviaDateTime } from './format-bolivia-date-time';

describe('formatBoliviaDateTime', () => {
  it('formats valid dates in Bolivia time', () => {
    const result = formatBoliviaDateTime('2026-09-01T12:30:00Z');

    expect(result).toContain('01/09/2026');
    expect(result).toContain('08:30');
  });

  it('returns an empty value for missing or invalid dates', () => {
    expect(formatBoliviaDateTime(null)).toBe('');
    expect(formatBoliviaDateTime('invalid')).toBe('');
  });
});
