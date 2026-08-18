import { Mantenimientos } from '@entities/admin';

export class MantenimientosAgrupados {
  datosgrupo: {
    Id: number;
    NombreEmpresaMantenimiento: string | null;
    FechaMantenimiento: Date | null;
    FechaFinalDeMantenimiento: Date | null;
    Costo: number | null;
    Descripcion: string | null;
    TipoMantenimiento: string | null;
    NombreGrupoEquipo: string | null;
    CodigoImtEquipo: string | null;
    DescripcionEquipo: string | null;
  };

  matenimientos: Mantenimientos[];

  constructor(matenimientos: Mantenimientos[]) {
    this.matenimientos = matenimientos;
    this.datosgrupo = {
      ...matenimientos[0],
      FechaMantenimiento: this.parseDateLocal(
        matenimientos[0].FechaMantenimiento,
      ),
      FechaFinalDeMantenimiento: this.parseDateLocal(
        matenimientos[0].FechaFinalDeMantenimiento,
      ),
      CodigoImtEquipo: this.toNullableString(matenimientos[0].CodigoImtEquipo),
    };

    for (let i = 1; i < matenimientos.length; i++) {
      this.datosgrupo.CodigoImtEquipo =
        this.datosgrupo.CodigoImtEquipo +
        ',' +
        String(matenimientos[i].CodigoImtEquipo);
    }
  }

  private parseDateLocal(date: Date | string | null): Date | null {
    if (!date) return null;
    if (date instanceof Date) return date;

    const parsed = new Date(date);
    if (!Number.isNaN(parsed.getTime())) return parsed;

    return null;
  }

  private toNullableString(value: number | string | null): string | null {
    return value === null ? null : String(value);
  }
}
