import { CartDateValidationService } from './cart-date-validation.service';

describe('CartDateValidationService', () => {
  const service = new CartDateValidationService();
  const currentDate = new Date('2026-08-12T08:00:00');

  it('accepts reservations lasting at least 30 minutes', () => {
    const result = service.validate(
      new Date('2026-08-12T09:00:00'),
      new Date('2026-08-12T09:30:00'),
      currentDate,
    );

    expect(result.isValid).toBeTrue();
  });

  it('rejects reservations shorter than 30 minutes', () => {
    const result = service.validate(
      new Date('2026-08-12T09:00:00'),
      new Date('2026-08-12T09:29:00'),
      currentDate,
    );

    expect(result.isValid).toBeFalse();
    expect(result.message).toContain('30 minutos');
  });
});
