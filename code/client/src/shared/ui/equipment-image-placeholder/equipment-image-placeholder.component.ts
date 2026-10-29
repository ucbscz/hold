import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-equipment-image-placeholder',
  standalone: true,
  templateUrl: './equipment-image-placeholder.component.html',
  styleUrl: './equipment-image-placeholder.component.css',
})
export class EquipmentImagePlaceholderComponent {
  @Input() label = 'Sin imagen';
}
