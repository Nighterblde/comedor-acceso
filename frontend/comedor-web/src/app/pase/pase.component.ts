import { Component, computed, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, EMPTY, interval, Subscription, switchMap, takeWhile, tap, timer } from 'rxjs';
import { MiTurno, ResultadoAcceso } from '../modelos/pase.model';
import { PaseService } from './pase.service';

export const TEXTO_ESCANEAR = 'Escanea de nuevo el código de la entrada';
export const TEXTO_REGISTRADO = 'Listo. El día que abra el comedor solo escanea el código de la entrada';

const INTERVALO_TURNO_MS = 3000;
const CLAVE_TOKEN_QR = 'comedor.pase.token';

type Vista = 'login' | 'cargando' | 'pase' | 'escanear' | 'registrado';

@Component({
  selector: 'app-pase',
  standalone: false,
  templateUrl: './pase.component.html',
  styleUrl: './pase.component.scss',
})
export class PaseComponent implements OnInit, OnDestroy {
  readonly textoEscanear = TEXTO_ESCANEAR;
  readonly textoRegistrado = TEXTO_REGISTRADO;

  readonly vista = signal<Vista>('cargando');
  readonly resultado = signal<ResultadoAcceso | null>(null);
  readonly turno = signal<MiTurno | null>(null);
  readonly textoTurno = computed(() => {
    const t = this.turno();
    switch (t?.estado) {
      case 'EnFila':
        return `Eres el ${t.posicion}.º en la fila`;
      case 'Sirviendo':
        return 'Te están sirviendo: pasa con el chef';
      case 'Servido':
        return 'Servido. ¡Buen provecho!';
      default:
        return '';
    }
  });
  readonly ahora = signal(new Date());
  readonly iniciales = computed(() =>
    (this.resultado()?.nombre ?? '')
      .split(/\s+/)
      .filter((p) => p.length > 0)
      .slice(0, 2)
      .map((p) => p[0].toUpperCase())
      .join(''),
  );

  punto: string | null = null;
  private token: string | null = null;
  private registro = false;
  private reloj?: Subscription;
  private consultaTurno?: Subscription;

  constructor(
    private readonly ruta: ActivatedRoute,
    private readonly pase: PaseService,
  ) {}

  ngOnInit(): void {
    this.token = this.leerToken();
    this.registro = this.ruta.snapshot.data['registro'] === true;
    this.punto = this.pase.puntoDeToken(this.token);
    this.reloj = interval(1000).subscribe(() => this.ahora.set(new Date()));
    this.avanzar();
  }

  ngOnDestroy(): void {
    this.reloj?.unsubscribe();
    this.consultaTurno?.unsubscribe();
  }

  /**
   * Toma el token del QR de la dirección y la deja limpia (/pase). Lo conserva en memoria y en
   * sessionStorage para que una recarga de la misma visita vuelva a validar.
   */
  private leerToken(): string | null {
    const deLaDireccion = this.ruta.snapshot.queryParamMap.get('t');
    if (deLaDireccion) {
      try {
        sessionStorage.setItem(CLAVE_TOKEN_QR, deLaDireccion);
      } catch {
        // Sin sessionStorage el token solo vive en memoria.
      }
      history.replaceState(history.state, '', window.location.pathname + window.location.hash);
      return deLaDireccion;
    }
    try {
      return sessionStorage.getItem(CLAVE_TOKEN_QR);
    } catch {
      return null;
    }
  }

  sesionIniciada(): void {
    this.avanzar();
  }

  /**
   * Cierra la sesión del empleado y vuelve al inicio de sesión. Conserva el código del QR de esta
   * visita (no es un dato de la cuenta) para poder entrar con otra cuenta sin volver a escanear.
   */
  cerrarSesion(): void {
    this.consultaTurno?.unsubscribe();
    this.pase.cerrarSesion();
    this.resultado.set(null);
    this.turno.set(null);
    this.vista.set('login');
  }

  private avanzar(): void {
    if (!this.pase.tieneSesion()) {
      this.vista.set('login');
    } else if (this.registro) {
      this.vista.set('registrado');
    } else if (!this.token) {
      this.vista.set('escanear');
    } else {
      this.validar(this.token);
    }
  }

  private validar(token: string): void {
    this.vista.set('cargando');
    this.pase.validar(token).subscribe({
      next: (r) => {
        this.resultado.set(r);
        this.vista.set(r.estado === 'EscanearDeNuevo' ? 'escanear' : 'pase');
        if (r.estado === 'Autorizado') {
          this.consultarTurno();
        }
      },
      error: (e) => {
        if (e?.status === 401) {
          this.pase.cerrarSesion();
          this.vista.set('login');
        } else {
          this.vista.set('escanear');
        }
      },
    });
  }

  /** Consulta el turno cada 3 s; se detiene con Servido o SinTurno y conserva el texto si una consulta falla. */
  private consultarTurno(): void {
    this.consultaTurno?.unsubscribe();
    this.consultaTurno = timer(0, INTERVALO_TURNO_MS)
      .pipe(
        switchMap(() => this.pase.miTurno().pipe(catchError(() => EMPTY))),
        tap((t) => this.turno.set(t)),
        takeWhile((t) => t.estado === 'EnFila' || t.estado === 'Sirviendo'),
      )
      .subscribe();
  }
}
