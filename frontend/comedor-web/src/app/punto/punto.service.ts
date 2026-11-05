import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SolicitudPunto } from '../modelos/consumo.model';
import { ResultadoAcceso, TokenQrRespuesta } from '../modelos/pase.model';

@Injectable({ providedIn: 'root' })
export class PuntoService {
  constructor(private readonly http: HttpClient) {}

  obtenerQr(puntoId: string): Observable<TokenQrRespuesta> {
    return this.http.get<TokenQrRespuesta>(
      `${environment.apiUrl}/api/puntos/${encodeURIComponent(puntoId)}/qr`,
    );
  }

  solicitudes(puntoId: string, desde?: string): Observable<SolicitudPunto[]> {
    let params = new HttpParams();
    if (desde) {
      params = params.set('desde', desde);
    }
    return this.http.get<SolicitudPunto[]>(
      `${environment.apiUrl}/api/puntos/${encodeURIComponent(puntoId)}/solicitudes`,
      { params },
    );
  }

  marcarServido(consumoId: string): Observable<{ yaServido: boolean }> {
    return this.http.post<{ yaServido: boolean }>(
      `${environment.apiUrl}/api/consumos/${encodeURIComponent(consumoId)}/servido`,
      {},
    );
  }

  capturaManual(matricula: string, puntoId: string): Observable<ResultadoAcceso> {
    return this.http.post<ResultadoAcceso>(`${environment.apiUrl}/api/accesos/manual`, {
      matricula,
      puntoId,
    });
  }
}
