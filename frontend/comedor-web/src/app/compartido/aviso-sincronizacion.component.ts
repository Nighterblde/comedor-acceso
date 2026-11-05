import { Component, OnInit, signal } from '@angular/core';
import { EstadoSincronizacion } from '../modelos/reporte.model';
import { SincronizacionService } from './sincronizacion.service';

/** Aviso de RNF-05: se muestra solo si la última sincronización con RR. HH. falló. */
@Component({
  selector: 'app-aviso-sincronizacion',
  standalone: false,
  template: `
    @if (estado()?.ultimaFallida) {
      <p class="aviso" role="status">
        <span class="icono" aria-hidden="true">⚠</span>
        La última sincronización con RR. HH. falló; se usan los datos del
        {{ estado()?.ultimaExitosa ? (estado()?.ultimaExitosa | date: 'dd/MM/yyyy') : '—' }}
      </p>
    }
  `,
  styles: `
    .aviso {
      display: flex;
      gap: 10px;
      align-items: center;
      margin: 0 0 20px;
      padding: 12px 16px;
      border: 1px solid var(--color-advertencia-borde);
      border-radius: var(--radio-control);
      background: var(--color-advertencia-fondo);
      color: var(--color-advertencia-texto);
      font-weight: 600;
    }
  `,
})
export class AvisoSincronizacionComponent implements OnInit {
  readonly estado = signal<EstadoSincronizacion | null>(null);

  constructor(private readonly sincronizacion: SincronizacionService) {}

  ngOnInit(): void {
    // Si la consulta falla, no se muestra aviso: este aviso no debe impedir usar la pantalla.
    this.sincronizacion.estado().subscribe({ next: (e) => this.estado.set(e), error: () => undefined });
  }
}
