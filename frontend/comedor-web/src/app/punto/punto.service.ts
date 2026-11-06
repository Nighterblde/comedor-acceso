import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { FilaServicio } from '../modelos/consumo.model';

@Injectable({ providedIn: 'root' })
export class PuntoService {
  constructor(private readonly http: HttpClient) {}

  fila(puntoId: string): Observable<FilaServicio> {
    return this.http.get<FilaServicio>(
      `${environment.apiUrl}/api/puntos/${encodeURIComponent(puntoId)}/fila`,
    );
  }

  marcarServido(consumoId: string): Observable<{ yaServido: boolean }> {
    return this.http.post<{ yaServido: boolean }>(
      `${environment.apiUrl}/api/consumos/${encodeURIComponent(consumoId)}/servido`,
      {},
    );
  }
}
