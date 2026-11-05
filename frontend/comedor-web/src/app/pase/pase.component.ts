import { Component, computed, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { ResultadoAcceso } from '../modelos/pase.model';
import { PaseService } from './pase.service';

export const TEXTO_ESCANEAR = 'Escanea de nuevo el código de la entrada';
export const TEXTO_REGISTRADO = 'Listo. El día que abra el comedor solo escanea el código de la entrada';

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

  constructor(
    private readonly ruta: ActivatedRoute,
    private readonly pase: PaseService,
  ) {}

  ngOnInit(): void {
    this.token = this.ruta.snapshot.queryParamMap.get('t');
    this.registro = this.ruta.snapshot.data['registro'] === true;
    this.punto = this.pase.puntoDeToken(this.token);
    this.reloj = interval(1000).subscribe(() => this.ahora.set(new Date()));
    this.avanzar();
  }

  ngOnDestroy(): void {
    this.reloj?.unsubscribe();
  }

  sesionIniciada(): void {
    this.avanzar();
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
}
