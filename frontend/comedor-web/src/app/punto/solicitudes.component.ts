import { Component, Input, OnDestroy, OnInit, signal } from '@angular/core';
import { catchError, of, Subscription, switchMap, timer } from 'rxjs';
import { SolicitudPunto } from '../modelos/consumo.model';
import { PuntoService } from './punto.service';

const MAX_RECHAZOS = 10;

@Component({
  selector: 'app-solicitudes',
  standalone: false,
  templateUrl: './solicitudes.component.html',
  styleUrl: './solicitudes.component.scss',
})
export class SolicitudesComponent implements OnInit, OnDestroy {
  @Input({ required: true }) puntoId!: string;

  readonly porServir = signal<SolicitudPunto[]>([]);
  readonly rechazos = signal<SolicitudPunto[]>([]);
  readonly sinConexion = signal(false);

  private readonly servidos = new Set<string>();
  private readonly enCurso = new Set<string>();
  private suscripcion?: Subscription;

  constructor(private readonly servicio: PuntoService) {}

  ngOnInit(): void {
    this.suscripcion = timer(0, 2000)
      .pipe(
        switchMap(() => this.servicio.solicitudes(this.puntoId).pipe(catchError(() => of(null)))),
      )
      .subscribe((lista) => {
        this.sinConexion.set(lista === null);
        if (lista) {
          this.actualizar(lista);
        }
      });
  }

  ngOnDestroy(): void {
    this.suscripcion?.unsubscribe();
  }

  servir(s: SolicitudPunto): void {
    const id = s.consumoId;
    if (!id || this.enCurso.has(id)) {
      return;
    }
    this.enCurso.add(id);
    this.servicio.marcarServido(id).subscribe({
      next: () => {
        this.enCurso.delete(id);
        this.servidos.add(id);
        this.porServir.update((l) => l.filter((x) => x.consumoId !== id));
      },
      error: () => this.enCurso.delete(id),
    });
  }

  private actualizar(lista: SolicitudPunto[]): void {
    // El servidor entrega lo más reciente primero; por servir va en orden de llegada.
    this.porServir.set(lista
      .filter((s) => s.autorizado && !s.servido && !!s.consumoId && !this.servidos.has(s.consumoId))
      .reverse());
    this.rechazos.set(lista.filter((s) => !s.autorizado).slice(0, MAX_RECHAZOS));
  }
}
