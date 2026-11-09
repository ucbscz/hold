import { BaseModel } from '@shared/model';
export class Carrera extends BaseModel {
  Nombre?: string | null;
  Codigo?: string | null;
  constructor() {
    super();
    this.Nombre = null;
    this.Codigo = null;
  }
}
