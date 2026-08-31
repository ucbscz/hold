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

  it('rejects reservations longer than the most restrictive group limit', () => {
    const result = service.validate(
      new Date('2026-08-12T09:00:00'),
      new Date('2026-08-14T09:01:00'),
      currentDate,
      2,
    );

    expect(result.isValid).toBeFalse();
    expect(result.message).toContain('2 día');
  });

  it('accepts a reservation that exactly matches the group limit', () => {
    const result = service.validate(
      new Date('2026-08-12T09:00:00'),
      new Date('2026-08-14T09:00:00'),
      currentDate,
      2,
    );

    expect(result.isValid).toBeTrue();
  });

  it('accepts the exact service-hour boundaries', () => {
    const result = service.validate(
      new Date('2026-08-12T17:30:00'),
      new Date('2026-08-12T18:00:00'),
      currentDate,
    );

    expect(result.isValid).toBeTrue();
  });

  it('rejects reservations before opening or after closing', () => {
    const beforeOpening = service.validate(
      new Date('2026-08-13T07:30:00'),
      new Date('2026-08-13T08:00:00'),
      currentDate,
    );
    const afterClosing = service.validate(
      new Date('2026-08-13T17:30:00'),
      new Date('2026-08-13T18:30:00'),
      currentDate,
    );

    expect(beforeOpening.isValid).toBeFalse();
    expect(afterClosing.isValid).toBeFalse();
    expect(beforeOpening.message).toContain('horario de atención');
    expect(afterClosing.message).toContain('horario de atención');
  });

  it('rejects reservations starting or ending on Sunday', () => {
    const result = service.validate(
      new Date('2026-08-16T09:00:00'),
      new Date('2026-08-16T09:30:00'),
      currentDate,
    );

    expect(result.isValid).toBeFalse();
    expect(result.message).toContain('horario de atención');
  });
});
