import { Component, EventEmitter, Input, Output } from '@angular/core';
@Component({
  selector: 'app-aviso-eliminar',
  imports: [],
  templateUrl: './aviso-eliminar.component.html',
  styleUrls: ['./aviso-eliminar.component.css'],
})
export class AvisoEliminarComponent {
  @Input() mensaje: string = '';
  @Output() aceptar: EventEmitter<void> = new EventEmitter<void>();
  @Output() cancelar: EventEmitter<void> = new EventEmitter<void>();
  onAceptar() {
    this.aceptar.emit();
  }
  onCancelar() {
    this.cancelar.emit();
  }
}
