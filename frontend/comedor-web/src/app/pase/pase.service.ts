import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../core/auth.service';
import { ResultadoAcceso } from '../modelos/pase.model';

export const MENSAJE_CREDENCIALES = 'Credenciales incorrectas';
export const MENSAJE_BLOQUEADO = 'Cuenta bloqueada por 15 minutos';
export const MENSAJE_SERVICIO = 'No se pudo verificar tu cuenta; acude con el encargado';

@Injectable({ providedIn: 'root' })
export class PaseService {
  constructor(
    private readonly http: HttpClient,
    private readonly auth: AuthService,
  ) {}

  validar(tokenQr: string): Observable<ResultadoAcceso> {
    return this.http.post<ResultadoAcceso>(`${environment.apiUrl}/api/accesos/celular`, { tokenQr });
  }

  tieneSesion(): boolean {
    return !!this.auth.tokenEmpleado();
  }

  cerrarSesion(): void {
    this.auth.cerrarSesionEmpleado();
  }

  /** Inicia sesión del empleado; el error emitido es un Error con el mensaje para el usuario. */
  iniciarSesion(id: string, contrasena: string): Observable<void> {
    return this.auth.iniciarSesionEmpleado(id, contrasena).pipe(
      catchError((e) => {
        const codigo = e?.status === 401 ? e?.error?.error : null;
        const mensaje =
          codigo === 'CredencialesIncorrectas'
            ? MENSAJE_CREDENCIALES
            : codigo === 'Bloqueado'
              ? MENSAJE_BLOQUEADO
              : MENSAJE_SERVICIO;
        return throwError(() => new Error(mensaje));
      }),
    );
  }

  /** Lee el punto ("P1") de la primera parte del token del QR; null si no se puede. */
  puntoDeToken(tokenQr: string | null): string | null {
    if (!tokenQr) {
      return null;
    }
    try {
      const base64 = tokenQr.split('.')[0].replace(/-/g, '+').replace(/_/g, '/');
      const relleno = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      const bytes = Uint8Array.from(atob(relleno), (c) => c.charCodeAt(0));
      const punto = new TextDecoder().decode(bytes).split('|')[0];
      return punto ? punto : null;
    } catch {
      return null;
    }
  }
}
