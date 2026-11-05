import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { EstadoSincronizacion } from '../modelos/reporte.model';

/**
 * Estado compartido de la sincronización para el aviso de RNF-05 (D-22). Quien sincroniza llama
 * `refrescar()` para que el aviso refleje el resultado sin recargar la página.
 */
@Injectable({ providedIn: 'root' })
export class SincronizacionService {
  readonly estado = signal<EstadoSincronizacion | null>(null);

  constructor(private readonly http: HttpClient) {}

  refrescar(): void {
    // Si la consulta falla, se conserva el estado anterior: el aviso no debe impedir usar la pantalla.
    this.http
      .get<EstadoSincronizacion>(`${environment.apiUrl}/api/sincronizacion/estado`)
      .subscribe({ next: (e) => this.estado.set(e), error: () => undefined });
  }
}
