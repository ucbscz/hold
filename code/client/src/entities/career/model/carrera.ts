import { BaseModel } from '@shared/model';
export class Carrera extends BaseModel {
  Nombre?: string | null;
  constructor() {
    super();
    this.Nombre = null;
  }
}
