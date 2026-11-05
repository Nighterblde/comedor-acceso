import { Component, Input, signal } from '@angular/core';
import { PuntoService } from './punto.service';

@Component({
  selector: 'app-captura-manual',
  standalone: false,
  templateUrl: './captura-manual.component.html',
  styleUrl: './captura-manual.component.scss',
})
export class CapturaManualComponent {
  @Input({ required: true }) puntoId!: string;

  readonly matricula = signal('');
  private enviando = false;

  constructor(private readonly servicio: PuntoService) {}

  enviar(): void {
    const matricula = this.matricula().trim();
    if (!matricula || this.enviando) {
      return;
    }
    this.enviando = true;
    this.servicio.capturaManual(matricula, this.puntoId).subscribe({
      next: () => {
        this.enviando = false;
        this.matricula.set('');
      },
      error: () => {
        this.enviando = false;
      },
    });
  }
}
