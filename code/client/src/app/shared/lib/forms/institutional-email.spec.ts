import { environment } from '@environments/environment';
import { INSTITUTIONAL_EMAIL } from './institutional-email';

describe('INSTITUTIONAL_EMAIL', () => {
  const pattern = new RegExp(INSTITUTIONAL_EMAIL.pattern);
  const domain = environment.institutionalEmailDomain;

  it('accepts only addresses from the configured domain', () => {
    expect(pattern.test(`ana.perez@${domain}`)).toBeTrue();
    expect(pattern.test('ana.perez@gmail.com')).toBeFalse();
    expect(pattern.test(`ana@${domain.replace('.', 'x')}`)).toBeFalse();
  });

  it('derives the suffix and placeholder from the configured domain', () => {
    expect(INSTITUTIONAL_EMAIL.suffix).toBe(`@${domain}`);
    expect(INSTITUTIONAL_EMAIL.placeholder.endsWith(`@${domain}`)).toBeTrue();
  });
});
