const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_FILE_SIZE = 5 * 1024 * 1024;
const MAX_DATA_URL_LENGTH = 650000;

export async function processIdentityImage(file: File): Promise<string> {
  if (!ALLOWED_IMAGE_TYPES.includes(file.type) || file.size > MAX_FILE_SIZE) {
    throw new Error('Selecciona una imagen JPG, PNG o WebP de hasta 5 MB.');
  }

  const bitmap = await createImageBitmap(file);
  try {
    const scale = Math.min(1, 1200 / Math.max(bitmap.width, bitmap.height));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(bitmap.width * scale);
    canvas.height = Math.round(bitmap.height * scale);
    const context = canvas.getContext('2d');
    if (!context) throw new Error('No se pudo procesar la imagen.');
    context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
    const dataUrl = canvas.toDataURL('image/jpeg', 0.8);
    if (dataUrl.length > MAX_DATA_URL_LENGTH) {
      throw new Error('La imagen sigue siendo demasiado grande.');
    }
    return dataUrl;
  } finally {
    bitmap.close();
  }
}

export function identityDataUrlToBase64(
  value: string | null | undefined,
): string | null {
  if (!value) return null;
  const separator = value.indexOf(',');
  return separator >= 0 ? value.slice(separator + 1) : value;
}

export function identityBase64ToDataUrl(
  value: string | null | undefined,
): string {
  if (!value) return '';
  return value.startsWith('data:') ? value : `data:image/jpeg;base64,${value}`;
}
