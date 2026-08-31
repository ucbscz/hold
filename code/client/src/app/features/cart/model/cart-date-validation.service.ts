import { ConfiguracionDto, horarioParaFecha } from '@entities/configuracion';
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
    config: ConfiguracionDto | null = null,
  ): CartDateValidationResult {
    if (!startDate || !endDate) {
      return { isValid: false, message: null };
    }

    const maximumStartDate = new Date(currentDate);
    maximumStartDate.setFullYear(currentDate.getFullYear() + 1);

    if (
      !Number.isFinite(startDate.getTime()) ||
      !Number.isFinite(endDate.getTime())
    )
      return { isValid: false, message: 'Selecciona fechas válidas' };

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

    const minimo = config?.TiempoMinimoReservaMinutos ?? 30;
    if (endDate.getTime() - startDate.getTime() < minimo * 60 * 1000) {
      return {
        isValid: false,
        message: `Error: El préstamo debe durar al menos ${minimo} minutos`,
      };
    }

    const inicio = horarioParaFecha(config, startDate);
    const fin = horarioParaFecha(config, endDate);
    const startMinutes = startDate.getHours() * 60 + startDate.getMinutes();
    const endMinutes = endDate.getHours() * 60 + endDate.getMinutes();
    if (
      !inicio.Abierto ||
      !fin.Abierto ||
      startMinutes < inicio.InicioMinutos ||
      startMinutes > inicio.FinMinutos - minimo ||
      endMinutes < fin.InicioMinutos ||
      endMinutes > fin.FinMinutos
    ) {
      return {
        isValid: false,
        message:
          'Error: Selecciona fechas y horas dentro del horario de atención configurado',
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
