import { Component, signal } from '@angular/core';
import { textoDeError } from '../compartido/errores';
import { ConsumoContingencia, Contingencia } from '../modelos/administracion.model';
import { AdminService } from './admin.service';

/** Captura de la lista en papel como consumos de canal Contingencia (RF-26, D-15). */
@Component({
  selector: 'app-contingencia',
  standalone: false,
  templateUrl: './contingencia.component.html',
  styleUrl: './admin-pestana.scss',
})
export class ContingenciaComponent {
  readonly error = signal<string | null>(null);
  readonly registrado = signal<ConsumoContingencia | null>(null);
  readonly enviando = signal(false);
  datos: Contingencia = { matricula: '', fechaServicio: '', tiempoComida: '', referenciaLista: '' };

  constructor(private readonly admin: AdminService) {}

  /** Todos los campos son obligatorios, incluida la referencia a la lista en papel. */
  get completo(): boolean {
    return Object.values(this.datos).every((v) => v.trim() !== '');
  }

  /** aaaa-MM-dd → dd/MM/aaaa sin pasar por Date, para no correr el día por la zona horaria. */
  fechaLegible(fecha: string): string {
    const [a, m, d] = fecha.split('-');
    return d && m && a ? `${d}/${m}/${a}` : fecha;
  }

  registrar(): void {
    if (!this.completo || this.enviando()) {
      return;
    }
    const cuerpo: Contingencia = {
      matricula: this.datos.matricula.trim(),
      fechaServicio: this.datos.fechaServicio,
      tiempoComida: this.datos.tiempoComida.trim(),
      referenciaLista: this.datos.referenciaLista.trim(),
    };
    this.error.set(null);
    this.registrado.set(null);
    this.enviando.set(true);
    this.admin.contingencia(cuerpo).subscribe({
      next: (guardado) => {
        this.registrado.set(guardado);
        this.datos = { ...this.datos, matricula: '' };
        this.enviando.set(false);
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.enviando.set(false);
      },
    });
  }
}
