import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  BitacoraSincronizacion,
  ConsumoContingencia,
  Contingencia,
  PuntoEntrada,
  ReglaDerecho,
  ResultadoSincronizacion,
  UsuarioEdicion,
  UsuarioSistema,
} from '../modelos/administracion.model';
import { TokenQrRespuesta } from '../modelos/pase.model';

/** Un método por ruta de administración (Tarea 10) y el QR del punto (D-21). */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly base = `${environment.apiUrl}/api/admin`;

  constructor(private readonly http: HttpClient) {}

  reglas(): Observable<ReglaDerecho[]> {
    return this.http.get<ReglaDerecho[]>(`${this.base}/reglas`);
  }

  guardarRegla(regla: ReglaDerecho): Observable<void> {
    return this.http.put<void>(`${this.base}/reglas`, regla);
  }

  eliminarRegla(turno: string, tiempoComida: string): Observable<void> {
    const params = new HttpParams().set('turno', turno).set('tiempoComida', tiempoComida);
    return this.http.delete<void>(`${this.base}/reglas`, { params });
  }

  puntos(): Observable<PuntoEntrada[]> {
    return this.http.get<PuntoEntrada[]>(`${this.base}/puntos`);
  }

  guardarPunto(punto: PuntoEntrada): Observable<void> {
    return this.http.put<void>(`${this.base}/puntos`, punto);
  }

  qrPunto(id: string): Observable<TokenQrRespuesta> {
    return this.http.get<TokenQrRespuesta>(`${environment.apiUrl}/api/puntos/${encodeURIComponent(id)}/qr`);
  }

  usuarios(): Observable<UsuarioSistema[]> {
    return this.http.get<UsuarioSistema[]>(`${this.base}/usuarios`);
  }

  guardarUsuario(usuario: UsuarioEdicion): Observable<void> {
    return this.http.put<void>(`${this.base}/usuarios`, usuario);
  }

  sincronizar(): Observable<ResultadoSincronizacion> {
    return this.http.post<ResultadoSincronizacion>(`${this.base}/sincronizacion`, {});
  }

  bitacora(): Observable<BitacoraSincronizacion[]> {
    return this.http.get<BitacoraSincronizacion[]>(`${this.base}/sincronizacion`);
  }

  /** Descarga la plantilla de personal (RF-36) como archivo. */
  plantillaPersonal(): Observable<void> {
    return this.http.get(`${this.base}/personal/plantilla.xlsx`, { responseType: 'blob' }).pipe(
      map((blob) => {
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = 'plantilla-personal.xlsx';
        enlace.click();
        URL.revokeObjectURL(url);
      }),
    );
  }

  /** Sube el Excel de personal y sincroniza (campo `archivo`; el navegador fija el Content-Type multipart). */
  importarPersonal(archivo: File): Observable<ResultadoSincronizacion> {
    const cuerpo = new FormData();
    cuerpo.append('archivo', archivo, archivo.name);
    return this.http.post<ResultadoSincronizacion>(`${this.base}/personal/importar`, cuerpo);
  }

  contingencia(datos: Contingencia): Observable<ConsumoContingencia> {
    return this.http.post<ConsumoContingencia>(`${this.base}/contingencia`, datos);
  }
}
