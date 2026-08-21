import { Injectable } from '@angular/core';
import { CartDateValidationResult } from './cart-date-validation-result';

@Injectable({
  providedIn: 'root',
})
export class CartDateValidationService {
  validate(
    startDate: Date | null,
    endDate: Date | null,
    currentDate: Date,
    maximumLoanDays: number | null = null,
  ): CartDateValidationResult {
    if (!startDate || !endDate) {
      return { isValid: false, message: null };
    }

    const maximumStartDate = new Date(currentDate);
    maximumStartDate.setFullYear(currentDate.getFullYear() + 1);

    if (startDate > endDate) {
      return {
        isValid: false,
        message:
          'Error: La fecha y hora de inicio no puede ser posterior a la fecha final',
      };
    }

    if (startDate < currentDate) {
      return {
        isValid: false,
        message:
          'Error: La fecha y hora de inicio no puede ser menor a la hora actual',
      };
    }

    if (maximumStartDate < startDate) {
      return {
        isValid: false,
        message:
          'Error: La fecha de inicio no puede ser mayor a un año desde la fecha actual',
      };
    }

    if (endDate.getTime() - startDate.getTime() < 30 * 60 * 1000) {
      return {
        isValid: false,
        message: 'Error: El préstamo debe durar al menos 30 minutos',
      };
    }

    if (
      maximumLoanDays != null &&
      endDate.getTime() - startDate.getTime() >
        maximumLoanDays * 24 * 60 * 60 * 1000
    ) {
      return {
        isValid: false,
        message: `Error: Los equipos seleccionados permiten un préstamo máximo de ${maximumLoanDays} día(s)`,
      };
    }

    return { isValid: true, message: null };
  }
}
