import { BaseModel } from '@shared/model';
export class EmpresaMantenimiento extends BaseModel {
  NombreEmpresa?: string | null;
  NombreResponsable?: string | null;
  ApellidoResponsable?: string | null;
  Telefono?: string | null;
  Nit?: string | null;
  Direccion?: string | null;
  constructor() {
    super();
    this.NombreEmpresa = null;
    this.NombreResponsable = null;
    this.ApellidoResponsable = null;
    this.Telefono = null;
    this.Nit = null;
    this.Direccion = null;
  }
}
