import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { EstadoSincronizacion } from '../modelos/reporte.model';

/** Consulta el estado de la sincronización para el aviso de RNF-05 (D-22). */
@Injectable({ providedIn: 'root' })
export class SincronizacionService {
  constructor(private readonly http: HttpClient) {}

  estado(): Observable<EstadoSincronizacion> {
    return this.http.get<EstadoSincronizacion>(`${environment.apiUrl}/api/sincronizacion/estado`);
  }
}
