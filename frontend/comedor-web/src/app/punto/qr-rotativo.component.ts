import { Component, Input, OnDestroy, OnInit, signal } from '@angular/core';
import { catchError, of, Subscription, switchMap, timer } from 'rxjs';
import { TokenQrRespuesta } from '../modelos/pase.model';
import { PuntoService } from './punto.service';

const SEGUNDOS_ROTACION = 30;

@Component({
  selector: 'app-qr-rotativo',
  standalone: false,
  templateUrl: './qr-rotativo.component.html',
  styleUrl: './qr-rotativo.component.scss',
})
export class QrRotativoComponent implements OnInit, OnDestroy {
  @Input({ required: true }) puntoId!: string;

  readonly qr = signal<TokenQrRespuesta | null>(null);
  readonly sinConexion = signal(false);
  readonly segundosRestantes = signal(SEGUNDOS_ROTACION);

  private readonly suscripciones = new Subscription();

  constructor(private readonly servicio: PuntoService) {}

  ngOnInit(): void {
    this.suscripciones.add(
      timer(0, SEGUNDOS_ROTACION * 1000)
        .pipe(
          switchMap(() => this.servicio.obtenerQr(this.puntoId).pipe(catchError(() => of(null)))),
        )
        .subscribe((qr) => {
          this.qr.set(qr);
          this.sinConexion.set(qr === null);
        }),
    );
    this.suscripciones.add(
      timer(0, 1000).subscribe((n) => {
        this.segundosRestantes.set(SEGUNDOS_ROTACION - (n % SEGUNDOS_ROTACION));
      }),
    );
  }

  ngOnDestroy(): void {
    this.suscripciones.unsubscribe();
  }
}
