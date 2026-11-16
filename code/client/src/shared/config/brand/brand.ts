import brand from './brand.json';

export const BRAND = brand;

const internalCode = brand.inventory.internalCodeLabel;

export const INVENTORY_LABELS = {
  internalCodeLabel: internalCode,
  organizationCodeLabel: brand.inventory.organizationCodeLabel,
  equipmentScope: brand.inventory.equipmentScope,
  internalCode: `Código ${internalCode}`,
  internalCodes: `Códigos ${internalCode}`,
  equipmentInternalCode: `Código ${internalCode} del Equipo`,
  equipmentInternalCodeLower: `Código ${internalCode} del equipo`,
  organizationCode: `Código ${brand.inventory.organizationCodeLabel}`,
  internalCodeOf: (code: number | string) => `${internalCode} ${code}`,
} as const;
