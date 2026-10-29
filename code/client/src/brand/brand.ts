import brand from './brand.json';

export const BRAND = brand;

const internalCode = brand.inventory.internalCodeLabel;

export const INVENTORY_LABELS = {
  internalCode: `Código ${internalCode}`,
  internalCodes: `Códigos ${internalCode}`,
  equipmentInternalCode: `Código ${internalCode} del Equipo`,
  organizationCode: `Código ${brand.inventory.organizationCodeLabel}`,
  internalCodeOf: (code: number | string) => `${internalCode} ${code}`,
} as const;
