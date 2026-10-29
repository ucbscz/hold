import { EXTERNAL_SERVICES } from '@external-services/external-services';
import { BRAND } from '@brand/brand';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-privacidad',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './privacidad.component.html',
  styleUrl: './terminos.component.css',
})
export class PrivacidadComponent {
  protected readonly servicios = EXTERNAL_SERVICES;
  protected readonly marca = BRAND;
}
