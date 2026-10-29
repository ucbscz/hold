import { BaseModel } from '@shared/model';
export class Categorias extends BaseModel {
  Nombre?: string | null;
  constructor() {
    super();
    this.Nombre = null;
  }
}
