import { Component, OnInit, signal } from '@angular/core';
import { textoDeError } from '../compartido/errores';
import { SincronizacionService } from '../compartido/sincronizacion.service';
import { BitacoraSincronizacion, ResultadoSincronizacion } from '../modelos/administracion.model';
import { AdminService } from './admin.service';

/** Sincronización bajo demanda con RR. HH. (RF-02) y su bitácora (RF-04). */
@Component({
  selector: 'app-sincronizacion',
  standalone: false,
  templateUrl: './sincronizacion.component.html',
  styleUrl: './admin-pestana.scss',
})
export class SincronizacionComponent implements OnInit {
  readonly bitacora = signal<BitacoraSincronizacion[]>([]);
  readonly resultado = signal<ResultadoSincronizacion | null>(null);
  readonly error = signal<string | null>(null);
  readonly sincronizando = signal(false);

  constructor(
    private readonly admin: AdminService,
    private readonly estado: SincronizacionService,
  ) {}

  ngOnInit(): void {
    this.cargar();
  }

  sincronizar(): void {
    this.error.set(null);
    this.sincronizando.set(true);
    this.admin.sincronizar().subscribe({
      next: (r) => {
        this.resultado.set(r);
        this.sincronizando.set(false);
        this.cargar();
        this.estado.refrescar();
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.sincronizando.set(false);
        this.estado.refrescar();
      },
    });
  }

  private cargar(): void {
    this.admin.bitacora().subscribe({ next: (b) => this.bitacora.set(b), error: (e) => this.error.set(textoDeError(e)) });
  }
}
