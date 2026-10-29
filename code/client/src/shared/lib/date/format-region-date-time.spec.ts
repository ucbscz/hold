import { formatRegionDateTime } from './format-region-date-time';

describe('formatRegionDateTime', () => {
  it('formats valid dates in the configured region time zone', () => {
    const result = formatRegionDateTime('2026-09-01T12:30:00Z');

    expect(result).toContain('01/09/2026');
    expect(result).toContain('08:30');
  });

  it('returns an empty value for missing or invalid dates', () => {
    expect(formatRegionDateTime(null)).toBe('');
    expect(formatRegionDateTime('invalid')).toBe('');
  });
});
