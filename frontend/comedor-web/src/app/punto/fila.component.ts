import { Component, HostListener, Input, OnDestroy, OnInit, signal } from '@angular/core';
import { catchError, merge, of, Subject, Subscription, switchMap, timer } from 'rxjs';
import { FilaServicio } from '../modelos/consumo.model';
import { PuntoService } from './punto.service';

const INTERVALO_CONSULTA_MS = 2000;
const BLOQUEO_TRAS_MARCAR_MS = 1500;

@Component({
  selector: 'app-fila',
  standalone: false,
  templateUrl: './fila.component.html',
  styleUrl: './fila.component.scss',
})
export class FilaComponent implements OnInit, OnDestroy {
  @Input() puntoId = '';
  @Input() modo: 'tv' | 'panel' = 'tv';

  readonly fila = signal<FilaServicio>({ actual: null, siguientes: [], total: 0 });
  readonly sinConexion = signal(false);

  private readonly refrescar$ = new Subject<void>();
  private consulta?: Subscription;
  private desbloqueo?: Subscription;
  private enCurso = false;
  private bloqueado = false;

  constructor(private readonly servicio: PuntoService) {}

  get restantes(): number {
    return Math.max(this.fila().total - 1, 0);
  }

  ngOnInit(): void {
    this.consulta = merge(timer(0, INTERVALO_CONSULTA_MS), this.refrescar$)
      .pipe(
        switchMap(() =>
          this.servicio.fila(this.puntoId).pipe(
            catchError(() => {
              this.sinConexion.set(true);
              return of(null);
            }),
          ),
        ),
      )
      .subscribe((fila) => {
        if (fila) {
          this.fila.set(fila);
          this.sinConexion.set(false);
        }
      });
  }

  ngOnDestroy(): void {
    this.consulta?.unsubscribe();
    this.desbloqueo?.unsubscribe();
  }

  @HostListener('document:keydown', ['$event'])
  alTeclear(evento: KeyboardEvent): void {
    if (this.modo === 'tv' && !evento.repeat) {
      this.servir();
    }
  }

  @HostListener('document:click')
  @HostListener('document:touchstart')
  alTocar(): void {
    if (this.modo === 'tv') {
      this.servir();
    }
  }

  /** Marca Servido a la persona mostrada; ignora la entrada si hay petición en curso o bloqueo vigente. */
  servir(): void {
    const actual = this.fila().actual;
    if (!actual?.consumoId || this.enCurso || this.bloqueado) {
      return;
    }
    this.enCurso = true;
    this.bloqueado = true;
    this.desbloqueo?.unsubscribe();
    this.desbloqueo = timer(BLOQUEO_TRAS_MARCAR_MS).subscribe(() => (this.bloqueado = false));
    const terminar = () => {
      this.enCurso = false;
      this.refrescar$.next();
    };
    this.servicio.marcarServido(actual.consumoId).subscribe({ next: terminar, error: terminar });
  }
}
