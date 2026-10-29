import { BRAND } from '@brand/brand';

const domain = BRAND.institutionalEmailDomain;
const escapedDomain = domain.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

export const INSTITUTIONAL_EMAIL = {
  suffix: `@${domain}`,
  pattern: `^[a-zA-Z0-9._%+\\-]+@${escapedDomain}$`,
  placeholder: `nombre.apellido@${domain}`,
} as const;
